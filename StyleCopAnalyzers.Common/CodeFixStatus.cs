﻿// Copyright (c) Dennis Fischer. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace StyleCopAnalyzers.Status.Common
{
    /// <summary>
    /// This enum is used to indicate whether or not a code fix is implemented
    /// </summary>
    public enum CodeFixStatus
    {
        /// <summary>
        /// This value indicates, that a code fix is implemented
        /// </summary>
        Implemented,

        /// <summary>
        /// This value indicates, that a code fix is not implemented and
        /// will not be implemented because it either can't be implemented
        /// or a code fix would not be able to fix it rationally.
        /// </summary>
        NotImplemented,

        /// <summary>
        /// This value indicates, that a code fix is not implemented because
        /// no one implemented it yet, or it is not yet decided if a code fix
        /// is going to be implemented in the future.
        /// </summary>
        NotYetImplemented
    }
}
