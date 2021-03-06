﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeGeneration;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Globalization
{
    public abstract class CA2101CodeFixProviderBase : CodeFixProviderBase
    {
        public sealed override IEnumerable<string> GetFixableDiagnosticIds()
        {
            return SpecializedCollections.SingletonEnumerable(Interoperability.PInvokeDiagnosticAnalyzer.CA2101);
        }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            return FxCopFixersResources.SpecifyMarshalingForPInvokeStringArguments;
        }

        protected const string CharSetText = "CharSet";
        protected const string LPWStrText = "LPWStr";
        protected const string UnicodeText = "Unicode";

        internal SyntaxNode CreateMarshalAsArgument(ISyntaxFactoryService syntaxFactoryService, INamedTypeSymbol unmanagedType)
        {
            return syntaxFactoryService.CreateMemberAccessExpression(
                syntaxFactoryService.CreateTypeReferenceExpression(unmanagedType), syntaxFactoryService.CreateIdentifierName(LPWStrText));
        }

        internal SyntaxNode CreateCharSetArgument(ISyntaxFactoryService syntaxFactoryService, INamedTypeSymbol charSetType)
        {
            return syntaxFactoryService.CreateMemberAccessExpression(
                syntaxFactoryService.CreateTypeReferenceExpression(charSetType), syntaxFactoryService.CreateIdentifierName(UnicodeText));
        }
    }
}
