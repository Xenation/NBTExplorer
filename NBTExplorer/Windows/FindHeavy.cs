using System;
using System.Collections.Generic;
using System.Windows.Forms;

using NBTExplorer.Model;

namespace NBTExplorer.Windows {
	public partial class FindHeavy : Form {

		private DataNode searchRoot;
		public DataNode searchResult { get; private set; }

		private int targetNameHash;

		public FindHeavy(DataNode searchRoot) {
			InitializeComponent();

			this.searchRoot = searchRoot;
			this.searchResult = null;
		}

		public string ListName {
			get {
				return listNameField.Text;
			}
		}

		public int CountThreshold {
			get {
				return Convert.ToInt32(countThresholdField.Value);
			}
		}

		private void btnFind_Click(object sender, EventArgs e) {
			targetNameHash = ListName.GetHashCode(); // Precompute the target list name's hash for a faster equality check
			searchResult = IterativeSearch(searchRoot, CountThreshold);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e) {
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private DataNode IterativeSearch(DataNode rootNode, int countThreshold) {
			List<RegionFileDataNode> regions = new List<RegionFileDataNode>();
			GatherAllRegions(rootNode, regions); // Gather all the regions under the selected directory

			IEnumerator<DataNode>[] nodeEnumeratorStack = new IEnumerator<DataNode>[256]; // Enumerator Stack used when exploring chunk sub nodes (fixed size for improved performance)

			// Explore every region
			foreach (RegionFileDataNode region in regions) {
				if (!region.IsExpanded) {
					region.Expand();
				}

				// Explore every chunk
				foreach (RegionChunkDataNode chunk in region.Nodes) {
					if (!chunk.IsExpanded) {
						chunk.Expand();
					}

					int currentDepth = 0;
					nodeEnumeratorStack[0] = chunk.Nodes.GetEnumerator();
					nodeEnumeratorStack[0].MoveNext();

					// Explore chunk's sub nodes
					while (currentDepth >= 0) {
						DataNode currentNode = nodeEnumeratorStack[currentDepth].Current;

						if (!currentNode.IsExpanded) {
							currentNode.Expand();
						}

						// Check if node is a tag list matching the search
						TagListDataNode listNode = currentNode as TagListDataNode;
						if (listNode != null && listNode.TagCount > countThreshold && listNode.NodeName != null) {
							int nodeNameHash = listNode.NodeName.GetHashCode();
							if (nodeNameHash == targetNameHash) {
								return listNode;
							}
						}

						if (!nodeEnumeratorStack[currentDepth].MoveNext()) {
							// Reached end of sibling nodes
							currentDepth--;
							currentNode.Collapse();
						} else if (currentNode.Nodes.Count > 0) {
							// Node has children to explore
							currentDepth++;
							nodeEnumeratorStack[currentDepth] = currentNode.Nodes.GetEnumerator();
							nodeEnumeratorStack[currentDepth].MoveNext();
						}

					}

					chunk.Collapse();

				}

				// Collapse the region to free some memory
				region.Collapse();
			}

			return null;
		}

		private void GatherAllRegions(DataNode rootNode, List<RegionFileDataNode> regions) {
			if (rootNode is DirectoryDataNode) {
				DirectoryDataNode dirNode = (DirectoryDataNode) rootNode;
				if (!dirNode.IsExpanded) {
					dirNode.Expand();
				}

				foreach (DataNode subNode in dirNode.Nodes) {
					if (subNode is RegionFileDataNode) {
						regions.Add((RegionFileDataNode) subNode);
					} else {
						GatherAllRegions(subNode, regions);
					}
				}
			} else if (rootNode is RegionFileDataNode) {
				regions.Add((RegionFileDataNode) rootNode);
			}
		}

	}
}
