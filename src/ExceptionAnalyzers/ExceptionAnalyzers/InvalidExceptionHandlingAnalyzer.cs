﻿using System.Collections.Immutable;
using System.Linq;
using ErrorProne.NET.CoreAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ErrorProne.NET.Exceptions
{
    /// <summary>
    /// Checks that `catch` block uses `ex.Message`.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ExceptionHandlingAnalyer : DiagnosticAnalyzerBase
    {
        public const string DiagnosticId = RuleIds.OnlyExceptionMessageWasObserved;

        internal const string Title = "Only ex.Message property was observed in exception block.";
        internal const string MessageFormat = "Only ex.Message property was observed in exception block!";
        internal const string Category = "CodeSmell";

        public ExceptionHandlingAnalyer() : base(DiagnosticId, Title)
        {
        }

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override void Initialize(AnalysisContext context)
        {
            // I don't know why yet, but selecting SytaxKind.CatchClause lead to very strange behavior:
            // AnalyzeSyntax method would called for a few times and the same warning would be added to diagnostic list!
            // Using IdentifierName syntax instead.
            context.RegisterSyntaxNodeAction(AnalyzeCatchBlock, SyntaxKind.CatchClause);
        }

        // Called when Roslyn encounters a catch clause.
        private void AnalyzeCatchBlock(SyntaxNodeAnalysisContext context)
        {
            var catchBlock = (CatchClauseSyntax) context.Node;

            if (catchBlock.Declaration != null && catchBlock.Declaration.CatchIsTooGeneric(context.SemanticModel))
            {
                var usages = context.SemanticModel.GetExceptionIdentifierUsages(catchBlock);
                if (usages.Count == 0)
                {
                    // Exception was not observed. Warning would be emitted by different rule
                    return;
                }

                // First of all we should find all usages for ex.Message
                var messageUsages = usages
                    .Select(id => new { Parent = id.Identifier.Parent as MemberAccessExpressionSyntax, Id = id.Identifier })
                    .Where(x => x.Parent != null && x.Parent.Name.GetText().ToString() == "Message")
                    .ToList();

                if (messageUsages.Count == 0)
                {
                    // There would be no warnings! No ex.Message usages 
                    return;
                }

                bool wasObserved =
                    usages.
                    Select(id => id.Identifier)
                    .Except(messageUsages.Select(x => x.Id))
                    .Any(u => u.Parent is ArgumentSyntax || // Exception object was used directly
                              u.Parent is AssignmentExpressionSyntax || // Was saved to field or local
                                                                     // or Inner exception was used
                              ((u.Parent as MemberAccessExpressionSyntax)?.Name?.Identifier)?.Text == "InnerException");

                // If exception object was "observed" properly!
                if (wasObserved)
                {
                    return;
                }

                foreach (var messageUsage in messageUsages)
                {
                    var location = Location.Create(context.Node.SyntaxTree,
                        TextSpan.FromBounds(messageUsage.Parent.Span.Start, messageUsage.Parent.Span.End));
                    context.ReportDiagnostic(
                        Diagnostic.Create(UnnecessaryWithSuggestionDescriptor, location));
                }
            }
        }
    }
}
