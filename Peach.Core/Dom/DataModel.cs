
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

using System.Linq;

namespace Peach.Core.Dom
{
	/// <summary>
	/// DataModel is just a top level Block.
	/// </summary>
	[Serializable]
	[DataElement("DataModel")]
	[PitParsable("DataModel")]
	[Parameter("name", typeof(string), "Model name", "")]
	[Parameter("ref", typeof(string), "Model to reference", "")]
	public class DataModel : Block
	{
		/// <summary>
		/// Dom parent of data model if any
		/// </summary>
		/// <remarks>
		/// A data model can be the child of two (okay three) different types,
		///   1. Dom (dom.datamodel collection)
		///   2. Action (Action.dataModel)
		///   3. ActionParam (Action.parameters[0].dataModel)
		///   
		/// This variable is one of those parent holders.
		/// </remarks>
		[NonSerialized]
		public Dom dom = null;

		/// <summary>
		/// Action parent of data model if any
		/// </summary>
		/// <remarks>
		/// A data model can be the child of two (okay three) different types,
		///   1. Dom (dom.datamodel collection)
		///   2. Action (Action.dataModel)
		///   3. ActionParam (Action.parameters[0].dataModel)
		///   
		/// This variable is one of those parent holders.
		/// </remarks>
		[NonSerialized]
		public Action action = null;

		[NonSerialized]
		private CloneCache cache = null;

		[NonSerialized]
		private bool cracking = false;

		public DataModel()
		{
			this.Invalidated += new InvalidatedEventHandler(DataModel_Invalidated);
		}

		public DataModel(string name)
			: base(name)
		{
			this.Invalidated += new InvalidatedEventHandler(DataModel_Invalidated);
		}

		[OnDeserialized]
		void OnDeserialized(StreamingContext context)
		{
			this.Invalidated += new InvalidatedEventHandler(DataModel_Invalidated);
		}

		void  DataModel_Invalidated(object sender, EventArgs e)
		{
			cache = null;
		}

		public override DataElement Clone()
		{
			if (cracking)
				return new CloneCache(this, this.name).Get();

			if (cache == null)
				cache = new CloneCache(this, this.name);

			var ret = cache.Get() as DataModel;
			ret.cache = this.cache;

			return ret;
		}

		public override void Crack(Cracker.DataCracker context, IO.BitStream data, long? size)
		{
			try
			{
				cache = null;
				cracking = true;
				base.Crack(context, data, size);
			}
			finally
			{
				cracking = false;
			}
		}

		public void GenerateBoundaryFile(string filename)
                {
                        long pos = 0;
                        Stack<DataElement> stack = new Stack<DataElement>();
                        using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter (filename)) {
                                DataElement root = this;
                                stack.Push (this);
                                // Execute the loop until all data elements (nodes) are traversed
                                while (stack.Count >0) {
                                        DataElement node = stack.Pop ();
                                        // If the current node is a DataElementContainer, get all child nodes and push
                                        // them on top of the stack
                                        if (node is DataElementContainer) {
						// Output the name of the current node and its boundary
						string strName = node.name;
                                                DataElement ancestor = node.parent;
                                                while (ancestor != null) {
                                                        strName = ancestor.name + "~" + strName;
                                                        ancestor = ancestor.parent;
                                                }

						if (node.isMutable) {
                                                	outputFile.WriteLine ("{0},{1},{2},{3}", pos, pos + node.Value.LengthBytes - 1, strName, "Enabled");
						} else {
							outputFile.WriteLine ("{0},{1},{2},{3}", pos, pos + node.Value.LengthBytes - 1, strName,"Disabled");
						}

                                                //Console.WriteLine ("Processing node: {0}", node.name);
                                                DataElementContainer container = (DataElementContainer)node;
                                                if (container.Count > 0) {
                                                        for (int i=container.Count-1; i>=0; i--) {
                                                                //Console.WriteLine ("Pushing to stack: {0}", container [i].name);
                                                                stack.Push (container [i]);
                                                        }
                                                }
                                        } else {
                                                // Output the name of the current node and its boundary
                                                // in case the node is mutable
                                                if (node.Value.LengthBytes > 0) {
                                                        if (node.isMutable) {                                           
                                                                string strName = node.name;
                                                                DataElement ancestor = node.parent;
                                                                while (ancestor != null) {
                                                                        strName = ancestor.name + "~" + strName;
                                                                        ancestor = ancestor.parent;
                                                                }
                                                                outputFile.WriteLine ("{0},{1},{2},{3}", pos, pos + node.Value.LengthBytes - 1, strName,"Enabled");
                                                                pos += node.Value.LengthBytes;
                                                        } else {
                                                                // If the Data Element is not mutable, just update the position
                                                                //Console.WriteLine ("DataElement: {0} is not mutable", node.name);
								string strName = node.name;
                                                                DataElement ancestor = node.parent;
                                                                while (ancestor != null) {
                                                                        strName = ancestor.name + "~" + strName;
                                                                        ancestor = ancestor.parent;
                                                                }
                                                                outputFile.WriteLine ("{0},{1},{2},{3}", pos, pos + node.Value.LengthBytes - 1, strName,"Disabled");

                                                                pos += node.Value.LengthBytes;
                                                        }
                                                }
                                        }
                                }
                        }
                }
	}
}

// end
