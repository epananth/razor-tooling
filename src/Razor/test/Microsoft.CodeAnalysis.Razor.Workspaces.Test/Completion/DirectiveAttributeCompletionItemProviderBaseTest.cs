﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    public class DirectiveAttributeCompletionItemProviderBaseTest : RazorIntegrationTestBase
    {
        internal override string FileKind => FileKinds.Component;

        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void IntersectsWithAttributeNameOrPrefix_AtPrefixLeadingEdge_ReturnsFalse()
        {
            // Arrange

            // <p| class=""></p>
            var location = new SourceSpan(2, 0);
            var prefixLocation = new TextSpan(2, 1);
            var attributeNameLocation = new TextSpan(3, 5);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.IntersectsWithAttributeNameOrPrefix(location, prefixLocation, attributeNameLocation);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IntersectsWithAttributeNameOrPrefix_WithinPrefix_ReturnsTrue()
        {
            // Arrange

            // <p | class=""></p>
            var location = new SourceSpan(3, 0);
            var prefixLocation = new TextSpan(2, 2);
            var attributeNameLocation = new TextSpan(4, 5);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.IntersectsWithAttributeNameOrPrefix(location, prefixLocation, attributeNameLocation);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IntersectsWithAttributeNameOrPrefix_NullPrefix_ReturnsFalse()
        {
            // Arrange

            // <svg xml:base="abc"xm| ></svg>
            var location = new SourceSpan(21, 0);
            TextSpan? prefixLocation = null;
            var attributeNameLocation = new TextSpan(4, 5);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.IntersectsWithAttributeNameOrPrefix(location, prefixLocation, attributeNameLocation);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IntersectsWithAttributeNameOrPrefix_AtNameLeadingEdge_ReturnsTrue()
        {
            // Arrange

            // <p |class=""></p>
            var location = new SourceSpan(3, 0);
            var prefixLocation = new TextSpan(2, 1);
            var attributeNameLocation = new TextSpan(3, 5);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.IntersectsWithAttributeNameOrPrefix(location, prefixLocation, attributeNameLocation);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IntersectsWithAttributeNameOrPrefix_WithinName_ReturnsTrue()
        {
            // Arrange

            // <p cl|ass=""></p>
            var location = new SourceSpan(5, 0);
            var prefixLocation = new TextSpan(2, 1);
            var attributeNameLocation = new TextSpan(3, 5);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.IntersectsWithAttributeNameOrPrefix(location, prefixLocation, attributeNameLocation);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IntersectsWithAttributeNameOrPrefix_OutsideOfNameAndPrefix_ReturnsFalse()
        {
            // Arrange

            // <p class=|""></p>
            var location = new SourceSpan(9, 0);
            var prefixLocation = new TextSpan(2, 1);
            var attributeNameLocation = new TextSpan(3, 5);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.IntersectsWithAttributeNameOrPrefix(location, prefixLocation, attributeNameLocation);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryGetAttributeInfo_NonAttribute_ReturnsFalse()
        {
            // Arrange
            var node = GetNodeAt("@DateTime.Now", 4);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetAttributeInfo(node, out _, out _, out _, out _, out _);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryGetAttributeInfo_EmptyAttribute_ReturnsFalse()
        {
            // Arrange
            var node = GetNodeAt("<p    >", 3);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetAttributeInfo(node, out _, out _, out _, out _, out _);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryGetAttributeInfo_PartialAttribute_ReturnsTrue()
        {
            // Arrange
            var node = GetNodeAt("<p bin>", 4);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetAttributeInfo(node, out var prefixLocation, out var name, out var nameLocation, out var parameterName, out var parameterNameLocation);

            // Assert
            Assert.True(result);
            Assert.Equal(new TextSpan(2, 1), prefixLocation);
            Assert.Equal("bin", name);
            Assert.Equal(new TextSpan(3, 3), nameLocation);
            Assert.Null(parameterName);
        }

        [Fact]
        public void TryGetAttributeInfo_PartialTransitionedAttribute_ReturnsTrue()
        {
            // Arrange
            var node = GetNodeAt("<p @>", 4);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetAttributeInfo(node, out var prefixLocation, out var name, out var nameLocation, out var parameterName, out var parameterNameLocation);

            // Assert
            Assert.True(result);
            Assert.Equal(new TextSpan(2, 1), prefixLocation);
            Assert.Equal("@", name);
            Assert.Equal(new TextSpan(3, 1), nameLocation);
            Assert.Null(parameterName);
        }

        [Fact]
        public void TryGetAttributeInfo_FullAttribute_ReturnsTrue()
        {
            // Arrange
            var node = GetNodeAt("<p foo=\"anything\">", 4);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetAttributeInfo(node, out var prefixLocation, out var name, out var nameLocation, out var parameterName, out var parameterNameLocation);

            // Assert
            Assert.True(result);
            Assert.Equal(new TextSpan(2, 1), prefixLocation);
            Assert.Equal("foo", name);
            Assert.Equal(new TextSpan(3, 3), nameLocation);
            Assert.Null(parameterName);
        }

        [Fact]
        public void TryGetAttributeInfo_PartialDirectiveAttribute_ReturnsTrue()
        {
            var node = GetNodeAt("<input type=\"text\" @bind />", 22);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetAttributeInfo(node, out var prefixLocation, out var name, out var nameLocation, out var parameterName, out var parameterNameLocation);

            // Assert
            Assert.True(result);
            Assert.Equal(new TextSpan(18, 1), prefixLocation);
            Assert.Equal("@bind", name);
            Assert.Equal(new TextSpan(19, 5), nameLocation);
            Assert.Null(parameterName);
        }

        [Fact]
        public void TryGetAttributeInfo_DirectiveAttribute_ReturnsTrue()
        {
            var node = GetNodeAt(@"<input type=""text"" @bind=""@CurrentDate"" />
@code {
    public DateTime CurrentDate { get; set; } = DateTime.Now;
}", 22);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetAttributeInfo(node, out var prefixLocation, out var name, out var nameLocation, out var parameterName, out var parameterNameLocation);

            // Assert
            Assert.True(result);
            Assert.Equal(new TextSpan(18, 1), prefixLocation);
            Assert.Equal("@bind", name);
            Assert.Equal(new TextSpan(19, 5), nameLocation);
            Assert.Null(parameterName);
        }

        [Fact]
        public void TryGetAttributeInfo_DirectiveAttributeWithParameter_ReturnsTrue()
        {
            var node = GetNodeAt(@"<input type=""text"" @bind:format=""MM/dd/yyyy"" @bind=""@CurrentDate"" />
@code {
    public DateTime CurrentDate { get; set; } = DateTime.Now;
}", 22);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetAttributeInfo(node, out var prefixLocation, out var name, out var nameLocation, out var parameterName, out var parameterNameLocation);

            // Assert
            Assert.True(result);
            Assert.Equal(new TextSpan(18, 1), prefixLocation);
            Assert.Equal("@bind", name);
            Assert.Equal(new TextSpan(19, 5), nameLocation);
            Assert.Equal("format", parameterName);
            Assert.Equal(new TextSpan(25, 6), parameterNameLocation);
        }

        [Fact]
        public void TryGetElementInfo_MarkupTagParent()
        {
            // Arrange
            var node = GetNodeAt("<p class='hello @DateTime.Now'>", 2);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetElementInfo(node.Parent, out var tagName, out var attributes);

            // Assert
            Assert.True(result);
            Assert.Equal("p", tagName);
            var attributeName = Assert.Single(attributes);
            Assert.Equal("class", attributeName);
        }

        [Fact]
        public void TryGetElementInfo_TagHelperParent()
        {
            // Arrange
            var node = GetNodeAt(@"<input type=""text"" @bind=""@CurrentDate"" />
@code {
    public DateTime CurrentDate { get; set; } = DateTime.Now;
}", 2);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetElementInfo(node.Parent, out var tagName, out var attributes);

            // Assert
            Assert.True(result);
            Assert.Equal("input", tagName);
            Assert.Equal(new[] { "type", "@bind" }, attributes);
        }

        [Fact]
        public void TryGetElementInfo_NoAttributes_ReturnsEmptyAttributeCollection()
        {
            // Arrange
            var node = GetNodeAt("<p>", 2);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetElementInfo(node.Parent, out _, out var attributes);

            // Assert
            Assert.True(result);
            Assert.Empty(attributes);
        }

        [Fact]
        public void TryGetElementInfo_SingleAttribute_ReturnsAttributeName()
        {
            // Arrange
            var node = GetNodeAt("<p class='hello @DateTime.Now'>", 2);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetElementInfo(node.Parent, out _, out var attributes);

            // Assert
            Assert.True(result);
            var attributeName = Assert.Single(attributes);
            Assert.Equal("class", attributeName);
        }

        [Fact]
        public void TryGetElementInfo_MixedAttributes_ReturnsStringifiedAttributesResult()
        {
            // Arrange
            var node = GetNodeAt(@"<input type=""text"" @bind:format=""MM/dd/yyyy"" something @bind=""@CurrentDate"" />
@code {
    public DateTime CurrentDate { get; set; } = DateTime.Now;
}", 2);

            // Act
            var result = DirectiveAttributeCompletionItemProviderBase.TryGetElementInfo(node.Parent, out _, out var attributes);

            // Assert
            Assert.True(result);
            Assert.Equal(new[] { "type", "@bind:format", "something", "@bind" }, attributes);
        }

        private RazorSyntaxNode GetNodeAt(string content, int index)
        {
            var result = CompileToCSharp(content, throwOnFailure: false);
            var syntaxTree = result.CodeDocument.GetSyntaxTree();
            var change = new SourceChange(new SourceSpan(index, 0), string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            return owner;
        }
    }
}
