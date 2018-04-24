﻿namespace ErrorProne.NET.Structs
{
    /// <summary>
    /// Diagnostitcs produced by this project.
    /// </summary>
    public static class DiagnosticIds
    {
        /// <nodoc />
        public const string MakeStructReadonlyDiagnosticId = "EPS01";

        /// <nodoc />
        public const string NonReadOnlyStructPassedAsInParameterDiagnosticId = "EPS02";
        
        /// <nodoc />
        public const string NonReadOnlyStructReturnedByReadOnlyRefDiagnosticId = "EPS03";

        /// <nodoc />
        public const string NonReadOnlyStructRefReadOnlyLocalDiagnosticId = "ESP04";

        /// <nodoc />
        public const string UseInModifierForReadOnlyStructDiagnosticId = "EPS05";

        /// <nodoc />
        public const string HiddenStructCopyDiagnosticId = "EPS06";
    }
}