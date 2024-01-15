﻿using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ErrorProne.NET.StructAnalyzers
{
    /// <summary>
    /// A fixer for <see cref="MakeStructReadOnlyAnalyzer"/>.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeStructReadOnlyCodeFixProvider)), Shared]
    public class MakeStructMemberReadOnlyCodeFixProvider : CodeFixProvider
    {
        public const string Title = "Make a struct member readonly";

        /// <inheritdoc />
        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: Title,
                        createChangedDocument: c => MakeReadOnlyAsync(context.Document, diagnostic.Location, c),
                        equivalenceKey: Title),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MakeStructMemberReadOnlyAnalyzer.Rule.Id);

        /// <inheritdoc />
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private static async Task<Document> MakeReadOnlyAsync(Document document, Location location, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Find the type declaration identified by the diagnostic.
            var memberDeclaration = root
                ?.FindToken(location.SourceSpan.Start)
                .Parent?.AncestorsAndSelf()
                .OfType<MemberDeclarationSyntax>()
                .FirstOrDefault();
            if (root is null || memberDeclaration is null)
            {
                return document;
            }

            var readonlyToken = SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
            SyntaxTokenList modifiers;
            int partialIndex = memberDeclaration.Modifiers.IndexOf(SyntaxKind.PartialKeyword);
            if (partialIndex != -1)
            {
                modifiers = memberDeclaration.Modifiers.Insert(partialIndex, readonlyToken);
            }
            else
            {
                modifiers = memberDeclaration.Modifiers.Add(readonlyToken);
            }

            var oldLeadingTrivia = memberDeclaration.GetLeadingTrivia();

            // just adding modifiers can yield to an invalid code.
            // For instance, for the case like
            // <inheritdoc />
            // int IFoo.X => 42;
            var newType = memberDeclaration.WithoutLeadingTrivia().WithModifiers(modifiers).WithLeadingTrivia(oldLeadingTrivia);

            return document.ReplaceSyntaxRoot(root.ReplaceNode(memberDeclaration, newType));
        }
    }
}