﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CaseCorrection
{
    internal static class CaseCorrector
    {
        /// <summary>
        /// The annotation normally used on nodes to request case correction.
        /// </summary>
        public static readonly SyntaxAnnotation Annotation = new SyntaxAnnotation();

        /// <summary>
        /// Case corrects all names found in the provided document.
        /// </summary>
        public static async Task<Document> CaseCorrectAsync(Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await CaseCorrectAsync(document, root.FullSpan, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Case corrects all names found in the spans of any nodes annotated with the provided
        /// annotation.
        /// </summary>
        public static async Task<Document> CaseCorrectAsync(Document document, SyntaxAnnotation annotation, CancellationToken cancellationToken = default(CancellationToken))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await CaseCorrectAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(n => n.Span), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Case corrects all names found in the span.
        /// </summary>
        public static async Task<Document> CaseCorrectAsync(Document document, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return await CaseCorrectAsync(document, SpecializedCollections.SingletonEnumerable(span), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Case corrects all names found in the provided spans.
        /// </summary>
        public static async Task<Document> CaseCorrectAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await document.Project.LanguageServices.GetService<ICaseCorrectionService>().CaseCorrectAsync(document, spans, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Case correct only things that don't require semantic information
        /// </summary>
        internal static SyntaxNode CaseCorrect(SyntaxNode root, IEnumerable<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken = default(CancellationToken))
        {
            return workspace.Services.GetLanguageServices(root.Language).GetService<ICaseCorrectionService>().CaseCorrect(root, spans, workspace, cancellationToken);
        }
    }
}
