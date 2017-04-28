//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.IdentityModel.Logging;

namespace Microsoft.IdentityModel.Xml
{
    internal class XmlTokenStreamWriter
    {
        IList<XmlTokenEntry> _entries;
        int _count;
        int _position;
        string _excludedElement;
        int? _excludedElementDepth;
        string _excludedElementNamespace;

        public XmlTokenStreamWriter(IList<XmlTokenEntry> entries,
                                     string excludedElement,
                                     int? excludedElementDepth,
                                     string excludedElementNamespace)
        {
            if (entries == null)
            {
                throw LogHelper.LogArgumentNullException(nameof(entries));
            }
            _entries = entries;
            _count = entries.Count;
            _excludedElement = excludedElement;
            _excludedElementDepth = excludedElementDepth;
            _excludedElementNamespace = excludedElementNamespace;
        }

        public int Count
        {
            get { return _count; }
        }

        public int Position
        {
            get { return _position; }
        }

        public XmlNodeType NodeType
        {
            get { return _entries[_position].NodeType; }
        }

        public bool IsEmptyElement
        {
            get { return _entries[_position].IsEmptyElement; }
        }

        public string Prefix
        {
            get { return _entries[_position]._prefix; }
        }

        public string LocalName
        {
            get { return _entries[_position]._localName; }
        }

        public string NamespaceUri
        {
            get { return _entries[_position]._namespaceUri; }
        }

        public string Value
        {
            get { return _entries[_position].Value; }
        }

        public string ExcludedElement
        {
            get { return _excludedElement; }
        }

        public string ExcludedElementNamespace
        {
            get { return _excludedElementNamespace; }
        }


        public bool MoveToFirst()
        {
            _position = 0;
            return _count > 0;
        }

        public bool MoveToFirstAttribute()
        {
            if (_position < Count - 1 && _entries[_position + 1].NodeType == XmlNodeType.Attribute)
            {
                _position++;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool MoveToNext()
        {
            if (_position < _count - 1)
            {
                _position++;
                return true;
            }
            return false;
        }

        public bool MoveToNextAttribute()
        {
            if (_position < _count - 1 && _entries[_position + 1].NodeType == XmlNodeType.Attribute)
            {
                _position++;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void WriteTo(XmlDictionaryWriter writer)
        {
            if (writer == null)
                throw LogHelper.LogExceptionMessage(new ArgumentNullException("writer"));

            if (!MoveToFirst())
                throw LogHelper.LogExceptionMessage(new InvalidOperationException("XmlTokenBufferIsEmpty"));

            int depth = 0;
            int recordedDepth = -1;
            bool include = true;
            do
            {
                switch (NodeType)
                {
                    case XmlNodeType.Element:
                        bool isEmpty = IsEmptyElement;
                        depth++;
                        if (include
                            && (null == _excludedElementDepth || _excludedElementDepth == (depth - 1))
                            && LocalName == _excludedElement
                            && NamespaceUri == _excludedElementNamespace)
                        {
                            include = false;
                            recordedDepth = depth;
                        }
                        if (include)
                        {
                            writer.WriteStartElement(Prefix, LocalName, NamespaceUri);
                        }
                        if (MoveToFirstAttribute())
                        {
                            do
                            {
                                if (include)
                                {
                                    writer.WriteAttributeString(Prefix, LocalName, NamespaceUri, Value);
                                }
                            }
                            while (MoveToNextAttribute());
                        }
                        if (isEmpty)
                        {
                            goto case XmlNodeType.EndElement;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (include)
                        {
                            writer.WriteEndElement();
                        }
                        else if (recordedDepth == depth)
                        {
                            include = true;
                            recordedDepth = -1;
                        }
                        depth--;
                        break;
                    case XmlNodeType.CDATA:
                        if (include)
                        {
                            writer.WriteCData(Value);
                        }
                        break;
                    case XmlNodeType.Comment:
                        if (include)
                        {
                            writer.WriteComment(Value);
                        }
                        break;
                    case XmlNodeType.Text:
                        if (include)
                        {
                            writer.WriteString(Value);
                        }
                        break;
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        if (include)
                        {
                            writer.WriteWhitespace(Value);
                        }
                        break;
                    case XmlNodeType.DocumentType:
                    case XmlNodeType.XmlDeclaration:
                        break;
                }
            }
            while (MoveToNext());
        }
    }
}