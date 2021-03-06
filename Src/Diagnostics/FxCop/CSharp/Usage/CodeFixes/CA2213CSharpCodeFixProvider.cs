﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Usage;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Usage
{
    /// <summary>
    /// CA2213: Disposable fields should be disposed
    /// </summary>
    [ExportCodeFixProvider(CA2213DiagnosticAnalyzer.RuleId, LanguageNames.CSharp)]
    public class CA2213CSharpCodeFixProvider : CA2213CodeFixProviderBase
    {
        internal override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, string diagnosticId, CancellationToken cancellationToken)
        {            
            //// We are going to add a call Dispose on fields:
            ////
            ////        public void Dispose()
            ////        {
            ////            A.Dispose();
            ////            ...
            ////        }

            var syntaxNode = nodeToFix as VariableDeclaratorSyntax;
            if (syntaxNode == null)
            {
                return Task.FromResult(document);
            }

            // find a Dispose method
            var member = syntaxNode.FirstAncestorOrSelf<ClassDeclarationSyntax>()
                .DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(n => n.Identifier.ValueText == CA2213DiagnosticAnalyzer.Dispose).FirstOrDefault();
            if (member == null)
            {
                return Task.FromResult(document);
            }

            var factory = document.GetLanguageService<ISyntaxFactoryService>();
            var symbol = model.GetDeclaredSymbol(syntaxNode);

            // handle a case where a local in the Dipose method with the same name by generating this (or ClassName) and simplifying it
            var path = symbol.IsStatic
                            ? factory.CreateIdentifierName(symbol.ContainingType.MetadataName)
                            : factory.CreateThisExpression();

            var statement =
                factory.CreateExpressionStatement(
                    factory.CreateInvocationExpression(
                        factory.CreateMemberAccessExpression(
                            factory.CreateMemberAccessExpression(path, factory.CreateIdentifierName(symbol.Name)).WithAdditionalAnnotations(Simplification.Simplifier.Annotation),
                                factory.CreateIdentifierName(CA2213DiagnosticAnalyzer.Dispose))));

            var newMember = member.AddBodyStatements((StatementSyntax)statement).WithAdditionalAnnotations(Formatter.Annotation);
            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(member, newMember)));
        }
    }
}