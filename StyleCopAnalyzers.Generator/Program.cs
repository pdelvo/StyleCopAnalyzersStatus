// Copyright (c) Dennis Fischer. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace StyleCopAnalyzers.Status.Generator
{
    using System;
    using Newtonsoft.Json;
    using System.Globalization;
    using System.Threading;

    /// <summary>
    /// The starting point of this application.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The starting point of this application.
        /// </summary>
        /// <param name="args">The command line parameters.</param>
        internal static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length < 1)
            {
                args = new[] { @"C:\StyleCopAnalyzers\StyleCopAnalyzers.sln" };
            }

            SolutionReader reader = SolutionReader.CreateAsync(args[0]).GetAwaiter().GetResult();

            var diagnostics = reader.GetDiagnosticsAsync().Result;

            diagnostics = diagnostics.Sort((a, b) => a.Id.CompareTo(b.Id));

            Console.WriteLine(JsonConvert.SerializeObject(diagnostics));
        }
    }
}
