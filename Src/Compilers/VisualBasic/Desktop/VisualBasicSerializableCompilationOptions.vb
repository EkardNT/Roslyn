﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.Serialization
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis.VisualBasic
    <Serializable>
    Public NotInheritable Class VisualBasicSerializableCompilationOptions
        Inherits SerializableCompilationOptions

        Private _options As VisualBasicCompilationOptions

        Private Const GlobalImportsString = "GlobalImports"
        Private Const RootNamespaceString = "RootNamespace"
        Private Const OptionStrictString = "OptionStrict"
        Private Const OptionInferString = "OptionInfer"
        Private Const OptionExplicitString = "OptionExplicit"
        Private Const OptionCompareTextString = "OptionCompareText"
        Private Const EmbedVbCoreRuntimeString = "EmbedVbCoreRuntime"
        Private Const ParseOptionsString = "ParseOptions"

        Sub New(options As VisualBasicCompilationOptions)
            If options Is Nothing Then
                Throw New ArgumentNullException("options")
            End If

            _options = options
        End Sub

        Friend Sub New(info As SerializationInfo, context As StreamingContext)
            Dim serializableOptions = DirectCast(info.GetValue(ParseOptionsString, GetType(VisualBasicSerializableParseOptions)), VisualBasicSerializableParseOptions)

            _options = New VisualBasicCompilationOptions(
                outputKind:=DirectCast(info.GetInt32(OutputKindString), OutputKind),
                moduleName:=info.GetString(ModuleNameString),
                mainTypeName:=info.GetString(MainTypeNameString),
                scriptClassName:=info.GetString(ScriptClassNameString),
                cryptoKeyContainer:=info.GetString(CryptoKeyContainerString),
                cryptoKeyFile:=info.GetString(CryptoKeyFileString),
                delaySign:=DirectCast(info.GetValue(DelaySignString, GetType(Boolean?)), Boolean?),
                optimize:=info.GetBoolean(OptimizeString),
                checkOverflow:=info.GetBoolean(CheckOverflowString),
                fileAlignment:=info.GetInt32(FileAlignmentString),
                baseAddress:=info.GetUInt64(BaseAddressString),
                platform:=DirectCast(info.GetInt32(PlatformString), Platform),
                generalDiagnosticOption:=DirectCast(info.GetInt32(GeneralDiagnosticOptionString), ReportDiagnostic),
                specificDiagnosticOptions:=DirectCast(info.GetValue(SpecificDiagnosticOptionsString, GetType(Dictionary(Of String, ReportDiagnostic))), Dictionary(Of String, ReportDiagnostic)).ToImmutableDictionary(),
                highEntropyVirtualAddressSpace:=info.GetBoolean(HighEntropyVirtualAddressSpaceString),
                debugInformationKind:=DirectCast(info.GetInt32(DebugInformationKindString), DebugInformationKind),
                subsystemVersion:=SubsystemVersion.Create(info.GetInt32(SubsystemVersionMajorString), info.GetInt32(SubsystemVersionMinorString)),
                concurrentBuild:=info.GetBoolean(ConcurrentBuildString),
                xmlReferenceResolver:=XmlFileResolver.Default,
                sourceReferenceResolver:=SourceFileResolver.Default,
                metadataReferenceResolver:=MetadataFileReferenceResolver.Default,
                metadataReferenceProvider:=MetadataFileReferenceProvider.Default,
                assemblyIdentityComparer:=DesktopAssemblyIdentityComparer.Default,
                strongNameProvider:=New DesktopStrongNameProvider(),
                metadataImportOptions:=DirectCast(info.GetByte(MetadataImportOptionsString), MetadataImportOptions),
                features:=DirectCast(info.GetValue(FeaturesString, GetType(String())), String()).AsImmutable(),
                globalImports:=DirectCast(info.GetValue(GlobalImportsString, GetType(String())), String()).Select(AddressOf GlobalImport.Parse),
                rootNamespace:=info.GetString(RootNamespaceString),
                optionStrict:=CType(info.GetInt32(OptionStrictString), OptionStrict),
                optionInfer:=info.GetBoolean(OptionInferString),
                optionExplicit:=info.GetBoolean(OptionExplicitString),
                optionCompareText:=info.GetBoolean(OptionCompareTextString),
                embedVbCoreRuntime:=info.GetBoolean(EmbedVbCoreRuntimeString),
                parseOptions:=If(serializableOptions IsNot Nothing, serializableOptions.Options, Nothing))
        End Sub

        Public Overrides Sub GetObjectData(info As SerializationInfo, context As StreamingContext)
            CommonGetObjectData(_options, info, context)

            info.AddValue(GlobalImportsString, _options.GlobalImports.Select(Function(g) g.Name).ToArray())
            info.AddValue(RootNamespaceString, _options.RootNamespace)
            info.AddValue(OptionStrictString, _options.OptionStrict)
            info.AddValue(OptionInferString, _options.OptionInfer)
            info.AddValue(OptionExplicitString, _options.OptionExplicit)
            info.AddValue(OptionCompareTextString, _options.OptionCompareText)
            info.AddValue(EmbedVbCoreRuntimeString, _options.EmbedVbCoreRuntime)
            info.AddValue(ParseOptionsString, If(_options.ParseOptions IsNot Nothing, New VisualBasicSerializableParseOptions(_options.ParseOptions), Nothing))
        End Sub

        Public Shadows ReadOnly Property Options As VisualBasicCompilationOptions
            Get
                Return _options
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonOptions As CompilationOptions
            Get
                Return _options
            End Get
        End Property
    End Class
End Namespace
