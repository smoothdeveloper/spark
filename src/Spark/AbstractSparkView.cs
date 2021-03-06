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
using System;
using System.Collections.Generic;
using System.IO;
using Spark.Spool;

namespace Spark
{
    public abstract class AbstractSparkView : ISparkView
    {
        public abstract void RenderView(TextWriter writer);
        public abstract Guid GeneratedViewId { get; }

        protected Dictionary<string, TextWriter> _content = new Dictionary<string, TextWriter>();
        protected IDictionary<string, string> _once = new Dictionary<string, string>();

        public virtual bool TryGetViewData(string name, out object value)
        {
            value = null;
            return false;
        }

        public Dictionary<string, TextWriter> Content { get { return _content; } }
        public TextWriter Output { get; private set; }        

        public IDisposable OutputScope(string name)
        {
            TextWriter writer;
            if (!_content.TryGetValue(name, out writer))
            {
                writer = new SpoolWriter();
                _content.Add(name, writer);
            }
            return new OutputScopeImpl(this, writer);
        }

        public IDisposable OutputScope(TextWriter writer)
        {
            return new OutputScopeImpl(this, writer);
        }

        public IDisposable OutputScope()
        {
            return new OutputScopeImpl(this, new SpoolWriter());
        }


        public bool Once(object flag)
        {
            var flagString = Convert.ToString(flag);
            if (_once.ContainsKey(flagString))
                return false;

            _once.Add(flagString, null);
            return true;
        }


        public class OutputScopeImpl : IDisposable
        {
            private readonly AbstractSparkView view;
            private readonly TextWriter previous;


            public OutputScopeImpl(AbstractSparkView view, TextWriter writer)
            {
                this.view = view;
                previous = view.Output;
                view.Output = writer;
            }

            public void Dispose()
            {
                view.Output = previous;
            }
        }
    }
}
