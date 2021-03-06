// Copyright 2008-2009 Louis DeJardin - http://whereslou.com
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 
using System.Linq;
using Spark.Parser.Markup;

namespace Spark.Compiler.NodeVisitors
{
    public class OnceAttributeVisitor : NodeVisitor<OnceAttributeVisitor.Frame>
    {
        public OnceAttributeVisitor(VisitorContext context)
            : base(context)
        {
        }

        public class Frame
        {
            public string ClosingName { get; set; }
            public int ClosingNameOutstanding { get; set; }
        }

        bool IsOnceAttribute(AttributeNode attr)
        {
            if (Context.Namespaces == NamespacesType.Unqualified)
                return attr.Name == "once";

            if (attr.Namespace != Constants.Namespace)
                return false;

            return NameUtility.GetName(attr.Name) == "once";
        }

        static SpecialNode CreateWrappingNode(AttributeNode conditionalAttr)
        {
            var fakeAttribute = new AttributeNode("once", conditionalAttr.Nodes);
            var fakeElement = new ElementNode("test", new[] { fakeAttribute }, false) { OriginalNode = conditionalAttr };
            return new SpecialNode(fakeElement);
        }

        protected override void Visit(ElementNode node)
        {
            var attr = node.Attributes.FirstOrDefault(IsOnceAttribute);
            if (attr != null)
            {
                var wrapping = CreateWrappingNode(attr);
                Nodes.Add(wrapping);

                node.Attributes.Remove(attr);
                wrapping.Body.Add(node);

                if (!node.IsEmptyElement)
                {
                    PushFrame(wrapping.Body, new Frame { ClosingName = node.Name, ClosingNameOutstanding = 1 });
                }
            }
            else if (string.Equals(node.Name, FrameData.ClosingName) && !node.IsEmptyElement)
            {
                ++FrameData.ClosingNameOutstanding;
                Nodes.Add(node);
            }
            else
            {
                Nodes.Add(node);
            }
        }

        protected override void Visit(EndElementNode node)
        {
            Nodes.Add(node);

            if (string.Equals(node.Name, FrameData.ClosingName))
            {
                --FrameData.ClosingNameOutstanding;
                if (FrameData.ClosingNameOutstanding == 0)
                {
                    PopFrame();
                }
            }
        }

        protected override void Visit(SpecialNode node)
        {
            var reconstructed = new SpecialNode(node.Element);

            var nqName = NameUtility.GetName(node.Element.Name);

            AttributeNode attr = null;
            if (nqName != "test" && nqName != "if" && nqName != "elseif" && nqName != "else")
                attr = reconstructed.Element.Attributes.FirstOrDefault(IsOnceAttribute);

            if (attr != null)
            {
                reconstructed.Element.Attributes.Remove(attr);

                var wrapping = CreateWrappingNode(attr);
                Nodes.Add(wrapping);
                PushFrame(wrapping.Body, new Frame());
            }

            Nodes.Add(reconstructed);
            PushFrame(reconstructed.Body, new Frame());
            Accept(node.Body);
            PopFrame();

            if (attr != null)
            {
                PopFrame();
            }
        }

        protected override void Visit(ExtensionNode node)
        {
            var reconstructed = new ExtensionNode(node.Element, node.Extension);

            var attr = reconstructed.Element.Attributes.FirstOrDefault(IsOnceAttribute);
            if (attr != null)
            {
                reconstructed.Element.Attributes.Remove(attr);

                var wrapping = CreateWrappingNode(attr);
                Nodes.Add(wrapping);
                PushFrame(wrapping.Body, new Frame());
            }

            Nodes.Add(reconstructed);
            PushFrame(reconstructed.Body, new Frame());
            Accept(node.Body);
            PopFrame();

            if (attr != null)
            {
                PopFrame();
            }
        }
    }
}
