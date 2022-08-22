﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Protocol;
using Microsoft.AspNetCore.Razor.LanguageServer.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Workspaces.Extensions;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion.Delegation
{
    [UseExportProvider]
    public class DelegatedCompletionListProviderTest : LanguageServerTestBase
    {
        public DelegatedCompletionListProviderTest()
        {
            Provider = TestDelegatedCompletionListProvider.Create();
            ClientCapabilities = new VSInternalClientCapabilities();
        }

        private TestDelegatedCompletionListProvider Provider { get; }

        private VSInternalClientCapabilities ClientCapabilities { get; }

        [Fact]
        public async Task ResponseRewritersGetExecutedInOrder()
        {
            // Arrange
            var completionContext = new VSInternalCompletionContext();
            var codeDocument = CreateCodeDocument("<");
            var documentContext = TestDocumentContext.From("C:/path/to/file.cshtml", codeDocument);
            var rewriter1 = new TestResponseRewriter(order: 100);
            var rewriter2 = new TestResponseRewriter(order: 20);
            var provider = TestDelegatedCompletionListProvider.Create(rewriter1, rewriter2);

            // Act
            var completionList = await provider.GetCompletionListAsync(absoluteIndex: 1, completionContext, documentContext, ClientCapabilities, CancellationToken.None);

            // Assert
            Assert.Collection(completionList.Items,
                item => Assert.Equal("20", item.Label),
                item => Assert.Equal("100", item.Label));
        }

        [Fact]
        public async Task HtmlDelegation_Invoked()
        {
            // Arrange
            var completionContext = new VSInternalCompletionContext() { TriggerKind = CompletionTriggerKind.Invoked };
            var codeDocument = CreateCodeDocument("<");
            var documentContext = TestDocumentContext.From("C:/path/to/file.cshtml", codeDocument, hostDocumentVersion: 1337);

            // Act
            await Provider.GetCompletionListAsync(absoluteIndex: 1, completionContext, documentContext, ClientCapabilities, CancellationToken.None);

            // Assert
            var delegatedParameters = Provider.DelegatedParams;
            Assert.NotNull(delegatedParameters);
            Assert.Equal(RazorLanguageKind.Html, delegatedParameters.ProjectedKind);
            Assert.Equal(new Position(0, 1), delegatedParameters.ProjectedPosition);
            Assert.Equal(CompletionTriggerKind.Invoked, delegatedParameters.Context.TriggerKind);
            Assert.Equal(1337, delegatedParameters.HostDocument.Version);
            Assert.Null(delegatedParameters.ProvisionalTextEdit);
        }

        [Fact]
        public async Task HtmlDelegation_TriggerCharacter()
        {
            // Arrange
            var completionContext = new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerKind = CompletionTriggerKind.TriggerCharacter,
                TriggerCharacter = "<",
            };
            var codeDocument = CreateCodeDocument("<");
            var documentContext = TestDocumentContext.From("C:/path/to/file.cshtml", codeDocument, hostDocumentVersion: 1337);

            // Act
            await Provider.GetCompletionListAsync(absoluteIndex: 1, completionContext, documentContext, ClientCapabilities, CancellationToken.None);

            // Assert
            var delegatedParameters = Provider.DelegatedParams;
            Assert.NotNull(delegatedParameters);
            Assert.Equal(RazorLanguageKind.Html, delegatedParameters.ProjectedKind);
            Assert.Equal(new Position(0, 1), delegatedParameters.ProjectedPosition);
            Assert.Equal(CompletionTriggerKind.TriggerCharacter, delegatedParameters.Context.TriggerKind);
            Assert.Equal(VSInternalCompletionInvokeKind.Typing, delegatedParameters.Context.InvokeKind);
            Assert.Equal(1337, delegatedParameters.HostDocument.Version);
            Assert.Null(delegatedParameters.ProvisionalTextEdit);
        }

        [Fact]
        public async Task HtmlDelegation_UnsupportedTriggerCharacter_TranslatesToInvoked()
        {
            // Arrange
            var completionContext = new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerKind = CompletionTriggerKind.TriggerCharacter,
                TriggerCharacter = "|",
            };
            var codeDocument = CreateCodeDocument("|");
            var documentContext = TestDocumentContext.From("C:/path/to/file.cshtml", codeDocument, hostDocumentVersion: 1337);

            // Act
            await Provider.GetCompletionListAsync(absoluteIndex: 1, completionContext, documentContext, ClientCapabilities, CancellationToken.None);

            // Assert
            var delegatedParameters = Provider.DelegatedParams;
            Assert.NotNull(delegatedParameters);
            Assert.Equal(RazorLanguageKind.Html, delegatedParameters.ProjectedKind);
            Assert.Equal(new Position(0, 1), delegatedParameters.ProjectedPosition);
            Assert.Equal(CompletionTriggerKind.Invoked, delegatedParameters.Context.TriggerKind);
            Assert.Equal(VSInternalCompletionInvokeKind.Typing, delegatedParameters.Context.InvokeKind);
            Assert.Equal(1337, delegatedParameters.HostDocument.Version);
            Assert.Null(delegatedParameters.ProvisionalTextEdit);
        }

        [Fact]
        public async Task CSharp_Invoked()
        {
            // Arrange & Act
            var completionList = await GetCompletionListAsync("@$$", CompletionTriggerKind.Invoked);

            // Assert
            Assert.Contains(completionList.Items, item => item.Label == "System");
        }

        [Fact]
        public async Task CSharp_At_TranslatesToInvoked_Triggered()
        {
            // Arrange & Act
            var completionList = await GetCompletionListAsync("@$$", CompletionTriggerKind.TriggerCharacter);

            // Assert
            Assert.Contains(completionList.Items, item => item.Label == "System");
        }

        [Fact]
        public async Task CSharp_Operator_Triggered()
        {
            // Arrange & Act
            var completionList = await GetCompletionListAsync("@(DateTime.$$)", CompletionTriggerKind.TriggerCharacter);

            // Assert
            Assert.Contains(completionList.Items, item => item.Label == "Now");
        }

        [Fact]
        public async Task RazorDelegation_Noop()
        {
            // Arrange
            var completionContext = new VSInternalCompletionContext() { TriggerKind = CompletionTriggerKind.Invoked };
            var codeDocument = CreateCodeDocument("@functions ");
            var documentContext = TestDocumentContext.From("C:/path/to/file.razor", codeDocument, hostDocumentVersion: 1337);

            // Act
            var completionList = await Provider.GetCompletionListAsync(absoluteIndex: 11, completionContext, documentContext, ClientCapabilities, CancellationToken.None);

            // Assert
            Assert.Null(completionList);
            var delegatedParameters = Provider.DelegatedParams;
            Assert.Null(delegatedParameters);
        }

        [Fact]
        public async Task ProvisionalCompletion_TranslatesToCSharpWithProvisionalTextEdit()
        {
            // Arrange
            var completionContext = new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerKind = CompletionTriggerKind.TriggerCharacter,
                TriggerCharacter = ".",
            };
            var codeDocument = CreateCodeDocument("@DateTime.");
            var documentContext = TestDocumentContext.From("C:/path/to/file.cshtml", codeDocument, hostDocumentVersion: 1337);

            // Act
            await Provider.GetCompletionListAsync(absoluteIndex: 10, completionContext, documentContext, ClientCapabilities, CancellationToken.None);

            // Assert
            var delegatedParameters = Provider.DelegatedParams;
            Assert.NotNull(delegatedParameters);
            Assert.Equal(RazorLanguageKind.CSharp, delegatedParameters.ProjectedKind);

            // Just validating that we're generating code in a way that's different from the top-level document. Don't need to be specific.
            Assert.True(delegatedParameters.ProjectedPosition.Line > 2);
            Assert.Equal(CompletionTriggerKind.TriggerCharacter, delegatedParameters.Context.TriggerKind);
            Assert.Equal(VSInternalCompletionInvokeKind.Typing, delegatedParameters.Context.InvokeKind);
            Assert.Equal(1337, delegatedParameters.HostDocument.Version);
            Assert.NotNull(delegatedParameters.ProvisionalTextEdit);
        }

        [Fact]
        public async Task DotTriggerInMiddleOfCSharpImplicitExpressionNotTreatedAsProvisional()
        {
            // Arrange
            var completionContext = new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerKind = CompletionTriggerKind.TriggerCharacter,
                TriggerCharacter = ".",
            };
            var codeDocument = CreateCodeDocument("@DateTime.Now");
            var documentContext = TestDocumentContext.From("C:/path/to/file.cshtml", codeDocument, hostDocumentVersion: 1337);

            // Act
            await Provider.GetCompletionListAsync(absoluteIndex: 10, completionContext, documentContext, ClientCapabilities, CancellationToken.None);

            // Assert
            var delegatedParameters = Provider.DelegatedParams;
            Assert.NotNull(delegatedParameters);
            Assert.Equal(RazorLanguageKind.CSharp, delegatedParameters.ProjectedKind);

            // Just validating that we're generating code in a way that's different from the top-level document. Don't need to be specific.
            Assert.True(delegatedParameters.ProjectedPosition.Line > 2);
            Assert.Equal(CompletionTriggerKind.TriggerCharacter, delegatedParameters.Context.TriggerKind);
            Assert.Equal(VSInternalCompletionInvokeKind.Typing, delegatedParameters.Context.InvokeKind);
            Assert.Equal(1337, delegatedParameters.HostDocument.Version);
            Assert.Null(delegatedParameters.ProvisionalTextEdit);
        }

        private class TestResponseRewriter : DelegatedCompletionResponseRewriter
        {
            private readonly int _order;

            public TestResponseRewriter(int order)
            {
                _order = order;
            }

            public override int Order => _order;

            public override Task<VSInternalCompletionList> RewriteAsync(VSInternalCompletionList completionList, int hostDocumentIndex, DocumentContext hostDocumentContext, DelegatedCompletionParams delegatedParameters, CancellationToken cancellationToken)
            {
                var completionItem = new VSInternalCompletionItem()
                {
                    Label = Order.ToString(),
                };
                completionList.Items = completionList.Items.Concat(new[] { completionItem }).ToArray();

                return Task.FromResult(completionList);
            }
        }

        private async Task<VSInternalCompletionList> GetCompletionListAsync(string content, CompletionTriggerKind triggerKind)
        {
            TestFileMarkupParser.GetPosition(content, out var output, out var cursorPosition);
            var codeDocument = CreateCodeDocument(output);
            var csharpSourceText = codeDocument.GetCSharpSourceText();
            var csharpDocumentUri = new Uri("C:/path/to/file.razor__virtual.g.cs");
            var serverCapabilities =  new ServerCapabilities()
            {
                CompletionProvider = new CompletionOptions
                {
                    ResolveProvider = true,
                    TriggerCharacters = new[] { " ", "(", "=", "#", ".", "<", "[", "{", "\"", "/", ":", "~" }
                }
            };
            await using var csharpServer = await CSharpTestLspServerHelpers.CreateCSharpLspServerAsync(
                csharpSourceText, csharpDocumentUri, serverCapabilities).ConfigureAwait(false);

            await csharpServer.OpenDocumentAsync(csharpDocumentUri, csharpSourceText.ToString()).ConfigureAwait(false);

            var triggerCharacter = triggerKind == CompletionTriggerKind.TriggerCharacter ? output[cursorPosition - 1].ToString() : null;
            var invocationKind = triggerKind == CompletionTriggerKind.TriggerCharacter ? VSInternalCompletionInvokeKind.Typing : VSInternalCompletionInvokeKind.Explicit;

            var completionContext = new VSInternalCompletionContext()
            {
                TriggerKind = triggerKind,
                TriggerCharacter = triggerCharacter,
                InvokeKind = invocationKind,
            };
            var documentContext = TestDocumentContext.From("C:/path/to/file.razor", codeDocument, hostDocumentVersion: 1337);
            var provider = TestDelegatedCompletionListProvider.Create(csharpServer);

            var completionList = await provider.GetCompletionListAsync(absoluteIndex: cursorPosition, completionContext, documentContext, ClientCapabilities, CancellationToken.None);
            return completionList;
        }
    }
}
