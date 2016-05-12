﻿// Copyright (c) Dennis Fischer. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace StyleCopAnalyzers.Status.Generator
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.MSBuild;
    using Common;

    /// <summary>
    /// A class that is used to parse the StyleCop.Analyzers solution to get an overview
    /// about the implemented diagnostics.
    /// </summary>
    public class SolutionReader
    {
        private static Regex diagnosticPathRegex = new Regex(@"(?<type>[A-Za-z]+)Rules\\(?<name>[A-Za-z0-9]+)\.cs$");
        private INamedTypeSymbol diagnosticAnalyzerTypeSymbol;
        private INamedTypeSymbol noCodeFixAttributeTypeSymbol;

        private Solution solution;
        private Project analyzerProject;
        private Project codeFixProject;
        private MSBuildWorkspace workspace;
        private Assembly analyzerAssembly;
        private Assembly codeFixAssembly;
        private Compilation analyzerCompilation;
        private Compilation codeFixCompilation;
        private ITypeSymbol booleanType;
        private Type batchFixerType;

        private SolutionReader()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                if (e.Name.Contains(this.AnalyzerProjectName))
                {
                    return this.analyzerAssembly;
                }

                return null;
            };
        }

        private string SlnPath { get; set; }

        private string AnalyzerProjectName { get; set; }

        private string CodeFixProjectName { get; set; }

        private ImmutableArray<CodeFixProvider> CodeFixProviders { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="SolutionReader"/> class.
        /// </summary>
        /// <param name="pathToSln">The path to the StyleCop.Analayzers sln</param>
        /// <param name="analyzerProjectName">The project name of the analyzer project</param>
        /// <param name="codeFixProjectName">The project name of the code fix project</param>
        /// <returns>A <see cref="Task{SolutionReader}"/> representing the asynchronous operation</returns>
        public static async Task<SolutionReader> CreateAsync(string pathToSln, string analyzerProjectName = "StyleCop.Analyzers", string codeFixProjectName = "StyleCop.Analyzers.CodeFixes")
        {
            SolutionReader reader = new SolutionReader();

            reader.SlnPath = pathToSln;
            reader.AnalyzerProjectName = analyzerProjectName;
            reader.CodeFixProjectName = codeFixProjectName;
            reader.workspace = MSBuildWorkspace.Create(properties: new Dictionary<string, string> { { "Configuration", "Release" } });

            await reader.InitializeAsync();

            return reader;
        }

        /// <summary>
        /// Analyzes the project and returns information about the diagnostics in it.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<ImmutableList<StyleCopDiagnostic>> GetDiagnosticsAsync()
        {
            var diagnostics = ImmutableList.CreateBuilder<StyleCopDiagnostic>();

            var syntaxTrees = this.analyzerCompilation.SyntaxTrees;

            foreach (var syntaxTree in syntaxTrees)
            {
                var match = diagnosticPathRegex.Match(syntaxTree.FilePath);
                if (!match.Success)
                {
                    continue;
                }

                string shortName = match.Groups["name"].Value;
                string noCodeFixReason = null;

                // Check if this syntax tree represents a diagnostic
                SyntaxNode syntaxRoot = await syntaxTree.GetRootAsync();
                SemanticModel semanticModel = this.analyzerCompilation.GetSemanticModel(syntaxTree);
                SyntaxNode classSyntaxNode = syntaxRoot.DescendantNodes().FirstOrDefault(x => x.IsKind(SyntaxKind.ClassDeclaration));

                if (classSyntaxNode == null)
                {
                    continue;
                }

                INamedTypeSymbol classSymbol = semanticModel.GetDeclaredSymbol(classSyntaxNode) as INamedTypeSymbol;

                if (!this.InheritsFrom(classSymbol, this.diagnosticAnalyzerTypeSymbol))
                {
                    continue;
                }

                if (classSymbol.IsAbstract)
                {
                    continue;
                }

                bool hasImplementation = HasImplementation(syntaxRoot);

                IEnumerable<DiagnosticDescriptor> descriptorInfos = this.GetDescriptorInstances(classSymbol);

                foreach (var descriptorInfo in descriptorInfos)
                {
                    var codeFixAndFixAllStatus = this.GetCodeFixAndFixAllStatus(descriptorInfo.Id, classSymbol, out noCodeFixReason);

                    CodeFixStatus codeFixStatus = codeFixAndFixAllStatus.Item1;
                    FixAllStatus fixAllStatus = codeFixAndFixAllStatus.Item2;

                    string status = this.GetStatus(classSymbol, syntaxRoot, semanticModel, descriptorInfo);
                    if (descriptorInfo.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))
                    {
                        continue;
                    }

                    var diagnostic = new StyleCopDiagnostic
                    {
                        Id = descriptorInfo.Id,
                        Category = descriptorInfo.Category,
                        HasImplementation = hasImplementation,
                        Status = status,
                        Name = shortName,
                        Title = descriptorInfo.Title.ToString(),
                        HelpLink = descriptorInfo.HelpLinkUri,
                        CodeFixStatus = codeFixStatus,
                        FixAllStatus = fixAllStatus,
                        NoCodeFixReason = noCodeFixReason
                    };
                    diagnostics.Add(diagnostic);
                }
            }

            return diagnostics.ToImmutable();
        }

        private static bool HasImplementation(SyntaxNode syntaxRoot)
        {
            bool hasImplementation = true;
            foreach (var trivia in syntaxRoot.DescendantTrivia())
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                {
                    if (trivia.ToFullString().Contains("TODO: Implement analysis"))
                    {
                        hasImplementation = false;
                    }
                }
            }

            return hasImplementation;
        }

        private async Task InitializeAsync()
        {
            this.solution = await this.workspace.OpenSolutionAsync(this.SlnPath);

            this.analyzerProject = this.solution.Projects.Single(x => x.Name == this.AnalyzerProjectName);
            this.analyzerCompilation = await this.analyzerProject.GetCompilationAsync();
            this.analyzerCompilation = this.analyzerCompilation.WithOptions(this.analyzerCompilation.Options.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            this.codeFixProject = this.solution.Projects.Single(x => x.Name == this.CodeFixProjectName);
            this.codeFixCompilation = await this.codeFixProject.GetCompilationAsync();
            this.codeFixCompilation = this.codeFixCompilation.WithOptions(this.codeFixCompilation.Options.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            this.booleanType = this.analyzerCompilation.GetSpecialType(SpecialType.System_Boolean);

            this.Compile();

            this.noCodeFixAttributeTypeSymbol = this.analyzerCompilation.GetTypeByMetadataName("StyleCop.Analyzers.NoCodeFixAttribute");
            this.diagnosticAnalyzerTypeSymbol = this.analyzerCompilation.GetTypeByMetadataName(typeof(DiagnosticAnalyzer).FullName);

            this.batchFixerType = this.codeFixAssembly.GetType("StyleCop.Analyzers.Helpers.CustomBatchFixAllProvider");

            this.InitializeCodeFixTypes();
        }

        private void InitializeCodeFixTypes()
        {
            var codeFixTypes = this.codeFixAssembly.GetTypes().Where(x => x.FullName.EndsWith("CodeFixProvider"));

            this.CodeFixProviders = ImmutableArray.Create(
                codeFixTypes
                .Where(t => t.GetCustomAttributes<ExportCodeFixProviderAttribute>().Any())
                .Select(t => Activator.CreateInstance(t, true))
                .OfType<CodeFixProvider>()
                .Where(x => x != null)
                .Where(x => x.GetType().Name != "SettingsFileCodeFixProvider")
                .ToArray());
        }

        private void Compile()
        {
            string path = Path.Combine(Path.GetDirectoryName(this.SlnPath), this.AnalyzerProjectName);
            var resources = ResourceReader.GetResourcesRecursive(path);
            this.analyzerAssembly = this.GetAssembly(this.analyzerCompilation, resources);

            this.codeFixAssembly = this.GetAssembly(this.codeFixCompilation, resources);
        }

        private Assembly GetAssembly(Compilation compilation, IEnumerable<ResourceDescription> manifestResources = null)
        {
            MemoryStream memStream = new MemoryStream();

            var emitResult = compilation.Emit(memStream, manifestResources: manifestResources);

            if (!emitResult.Success)
            {
                throw new CompilationFailedException();
            }

            return Assembly.Load(memStream.ToArray());
        }

        private string GetStatus(INamedTypeSymbol classSymbol, SyntaxNode root, SemanticModel model, DiagnosticDescriptor descriptor)
        {
            // Some analyzers use multiple descriptors. We analyze the first one and hope that
            // thats enough.
            var members = GetDescriptors(classSymbol);

            foreach (var member in members)
            {
                ObjectCreationExpressionSyntax initializer = GetObjectCreationExpression(root, member);

                if (initializer == null)
                {
                    continue;
                }

                var firstArgument = initializer.ArgumentList.Arguments[0];

                string constantValue = (string)model.GetConstantValue(firstArgument.Expression).Value;

                if (constantValue != descriptor.Id)
                {
                    continue;
                }

                // We use the fact that the only parameter that returns a boolean is the one we are interested in
                var enabledByDefaultParameter = from argument in initializer.ArgumentList.Arguments
                                                where model.GetTypeInfo(argument.Expression).Type == this.booleanType
                                                select argument.Expression;
                var parameter = enabledByDefaultParameter.FirstOrDefault();
                string parameterString = parameter.ToString();
                var analyzerConstantLength = "AnalyzerConstants.".Length;

                if (parameterString.Length < analyzerConstantLength)
                {
                    return parameterString;
                }

                return parameter.ToString().Substring(analyzerConstantLength);
            }

            return "Unknown";
        }

        private static ObjectCreationExpressionSyntax GetObjectCreationExpression(SyntaxNode root, ISymbol member)
        {
            SyntaxNode node = root.FindNode(member.Locations.FirstOrDefault().SourceSpan);

            ObjectCreationExpressionSyntax initializer = null;
            if (node != null)
            {
                initializer = (node as PropertyDeclarationSyntax)?.Initializer?.Value as ObjectCreationExpressionSyntax;
                if (initializer == null)
                {
                    initializer = (node as VariableDeclaratorSyntax)?.Initializer?.Value as ObjectCreationExpressionSyntax;
                }
            }

            return initializer;
        }

        /// <summary>
        /// Finds all the symbols of <see cref="DiagnosticDescriptor"/>s in the type
        /// </summary>
        /// <param name="type">The type where the descriptors should be searched in.</param>
        /// <returns>An array of symbols of the descriptors.</returns>
        private static ISymbol[] GetDescriptors(INamedTypeSymbol type)
        {
            return type.GetMembers().Where(x => x.Name.Contains("Descriptor")).ToArray();
        }

        /// <summary>
        /// Gets the set of diagnostic descriptors that can be reported by the analyzer.
        /// </summary>
        /// <param name="diagnosticAnalyzer">A symbol of the analyzer type.</param>
        /// <returns>The set of diagnostic descriptors that can be reported by the analyzer.</returns>
        private ImmutableArray<DiagnosticDescriptor> GetDescriptorInstances(INamedTypeSymbol diagnosticAnalyzer)
        {
            var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(this.analyzerAssembly.GetType(diagnosticAnalyzer.ToString()));

            return analyzer.SupportedDiagnostics;
        }

        private Tuple<CodeFixStatus, FixAllStatus> GetCodeFixAndFixAllStatus(string diagnosticId, INamedTypeSymbol classSymbol, out string noCodeFixReason)
        {
            CodeFixStatus codeFixStatus;
            FixAllStatus fixAllStatus;

            noCodeFixReason = null;

            var noCodeFixAttribute = classSymbol
                .GetAttributes()
                .SingleOrDefault(x => x.AttributeClass == this.noCodeFixAttributeTypeSymbol);

            bool hasCodeFix = noCodeFixAttribute == null;
            if (!hasCodeFix)
            {
                codeFixStatus = CodeFixStatus.NotImplemented;
                fixAllStatus = FixAllStatus.None;
                if (noCodeFixAttribute.ConstructorArguments.Length > 0)
                {
                    noCodeFixReason = noCodeFixAttribute.ConstructorArguments[0].Value as string;
                }
            }
            else
            {
                // Check if the code fix actually exists
                var codeFixes = this.CodeFixProviders
                    .Where(x => x.FixableDiagnosticIds.Contains(diagnosticId))
                    .Select(x => IsBatchFixer(x))
                    .Where(x => x != null)
                    .Select(x => (bool)x).ToArray();

                hasCodeFix = codeFixes.Length > 0;

                codeFixStatus = hasCodeFix ? CodeFixStatus.Implemented : CodeFixStatus.NotYetImplemented;

                if (codeFixes.Any(x => x))
                {
                    fixAllStatus = FixAllStatus.BatchFixer;
                }
                else
                {
                    fixAllStatus = FixAllStatus.CustomImplementation;
                }
            }

            return new Tuple<CodeFixStatus, FixAllStatus>(codeFixStatus, fixAllStatus);
        }

        private bool? IsBatchFixer(CodeFixProvider provider)
        {
            var fixAllProvider = provider.GetFixAllProvider();

            if (fixAllProvider == null)
            {
                return null;
            }
            else
            {
                return fixAllProvider.GetType() == this.batchFixerType;
            }
        }

        private bool InheritsFrom(INamedTypeSymbol declaration, INamedTypeSymbol possibleBaseType)
        {
            while (declaration != null)
            {
                if (declaration == possibleBaseType)
                {
                    return true;
                }

                declaration = declaration.BaseType;
            }

            return false;
        }
    }
}