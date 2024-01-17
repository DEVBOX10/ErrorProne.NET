﻿; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
EPS01 | Performance | Warning | MakeStructReadOnlyAnalyzer
EPS05 | Performance | Info | UseInModifierForReadOnlyStructAnalyzer
EPS06 | Performance | Warning | HiddenStructCopyAnalyzer
EPS09 | Usage | Info | ExplicitInParameterAnalyzer
EPS10 | CodeSmell | Warning | DoNotCreateStructWithNoDefaultStructConstructionAttributeAnalyzer
EPS11 | CodeSmell | Warning | DoNotEmbedStructsWithNoDefaultStructConstructionAttributeAnalyzer
EPS12 | Performance | Warning | MakeStructMemberReadOnlyAnalyzer
EPS13 | Usage | Warning | NonDefaultableStructDeclarationAnalyzer
