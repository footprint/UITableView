using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UITableView : MonoBehaviour {
	const float threshold = 0.1f;
	//direction
//	public bool vertical = true;
//	public bool horizontal = false; //not supported yet!

	public UITableViewDelegate Delegate {get;set;}
	public UITableViewDataSource Datasource {get;set;}

	float offsetY = 0;

	ScrollRect scrollRect;
	RectTransform rcPanel;
	RectTransform rcContent;

	List<UITableViewCell> usedCells = new List<UITableViewCell>();
	List<UITableViewCell> unUsedCells = new List<UITableViewCell>();
	
	void Awake() {
		rcPanel = GetComponent<RectTransform>();
		scrollRect = GetComponent<ScrollRect>();
		rcContent = scrollRect.content;
	}

	void Start() {
		ReloadData();
	}

	void ResetData() {
		offsetY = 0;
	}

	int IndexFromOffset(float offset) {
		float h = 0;
		uint num = this.Datasource.NumberOfCells(this);
		for (int i = 0; i < num; ++i) {
			h += this.Delegate.HeightForIndex(this, i);

			if (h > offset) {
				return i;
			}
		}
		if (num > 0) {
			return ((int)(num - 1));
		}
		return -1; //out of range
	}

	void InsertCellAtIndex(UITableViewCell cell, int index) {
		cell.index = index;
		cell.transform.SetParent(rcContent.transform, false);
		cell.enabled = true;

		for (int i = usedCells.Count - 1; i >= 0; --i) {
			UITableViewCell _cell = usedCells[i];
			if (cell.index > _cell.index) {
				usedCells.Insert(i + 1, cell);
				return;
			}
		}
		usedCells.Insert(0, cell);
	}

	void MoveCellOutOfSight(UITableViewCell cell) {
		cell.index = -1;
		cell.transform.SetParent(null, false);
		cell.enabled = false;
		unUsedCells.Add(cell);
		usedCells.Remove(cell);
	}

	void RemoveUnvisibleCells(int from, int to) {
		if (usedCells.Count > 0) {
			if (-1 != from) {
				for (int i = usedCells.Count - 1; i >= 0; --i) {
					UITableViewCell cell = usedCells[i];
					if (cell.index < from) {
						MoveCellOutOfSight(cell);
					}
				}
			}
			if (-1 != to) {
				for (int i = usedCells.Count - 1; i >= 0; --i) {
					UITableViewCell cell = usedCells[i];
					if (cell.index > to) {
						MoveCellOutOfSight(cell);
					}
				}
			}
		}
	}

	void ShowVisibleCells(int from, int to) {
		if (0 == usedCells.Count) return;

		if (from != -1) {
			UITableViewCell _cell = usedCells[0];
			int begin = _cell.index;
			float y = _cell.rcTransform.anchoredPosition.y;
//			Debug.Log("from:"+from+"--> begin:"+begin);
			for (int i = begin - 1; i >= from; --i) {
				UITableViewCell cell = this.Datasource.CellForIndex(this, i);
				if (cell) {
					InsertCellAtIndex(cell, i);

					float h = this.Delegate.HeightForIndex(this, i); //cell.rcTransform.sizeDelta.y;
					y += h;
					
					cell.rcTransform.anchoredPosition = new Vector2(0, y);
				}
			}
		}
		if (to != -1) {
			UITableViewCell _cell = usedCells[usedCells.Count - 1];
			int end = _cell.index;
			float y = _cell.rcTransform.anchoredPosition.y;
			y -= this.Delegate.HeightForIndex(this, usedCells.Count - 1); //cell.rcTransform.sizeDelta.y;
//			Debug.Log("end+1:"+(end+1)+"--> to:"+to);
			for (int i = end + 1; i <= to; ++i) {
				UITableViewCell cell = this.Datasource.CellForIndex(this, i);
				if (cell) {
					InsertCellAtIndex(cell, i);

					cell.rcTransform.anchoredPosition = new Vector2(0, y);
					
					float h = this.Delegate.HeightForIndex(this, i); //cell.rcTransform.sizeDelta.y;
					y -= h;
				}
			}
		}
	}

	public void ReloadData() {
		ResetData();

		for (int i = usedCells.Count - 1; i >= 0; --i) {
			UITableViewCell cell = usedCells[i];
			MoveCellOutOfSight(cell);
		}

		float height = 0;
		uint num = this.Datasource.NumberOfCells(this);
		for (int i = 0; i < num; ++i) {
			height += this.Delegate.HeightForIndex(this, i);
		}
		
		Vector2 size = rcContent.sizeDelta;
		size.y = height;
		rcContent.sizeDelta = size;

		float y = 0;
		float maxY = rcPanel.sizeDelta.y;
		Transform tranContent = rcContent.transform;
		for (int i = 0; i < num; ++i) {
			UITableViewCell cell = this.Datasource.CellForIndex(this, i);
			if (cell) {
				usedCells.Add(cell);
				cell.index = i;
				cell.transform.SetParent(tranContent, false);
				cell.rcTransform.anchoredPosition = new Vector2(0, y);
				cell.enabled = true;

				float h = this.Delegate.HeightForIndex(this, i); //cell.rcTransform.sizeDelta.y;
				y -= h;

				if (-y > maxY || Mathf.Approximately(-y, maxY)) {
					break;
				}
			}else {
				break;
			}
		}
	}

	public UITableViewCell CellForIndex(int index) {
		for (int i = 0; i < usedCells.Count; ++i) {
			UITableViewCell cell = usedCells[i];
			if (cell.index == index) {
				return cell;
			}
		}
		return null;
	}

	public UITableViewCell DequeueReusableCell() {
		if (unUsedCells.Count > 0) {
			UITableViewCell cell = unUsedCells[unUsedCells.Count - 1];
			unUsedCells.RemoveAt(unUsedCells.Count - 1);
			return cell;
		}
		return null;
	}

	public UITableViewCell DequeueReusableCellWithIdentifier(string identifier) {
		for (int i = 0; i < unUsedCells.Count; ++i) {
			UITableViewCell cell = unUsedCells[i];
			if (cell.identifier == identifier) {
				unUsedCells.RemoveAt(i);
				return cell;
			}
		}
		return null;
	}

	//scrollrect event
	public void onScrollChanged(Vector2 pos) {
		float y = rcContent.anchoredPosition.y;
		float maxY = rcContent.sizeDelta.y - rcPanel.sizeDelta.y;

		if (y < threshold) {
			y = threshold;
		}else if (y > (maxY + threshold)) {
			y = maxY + threshold;
		}

		if (!Mathf.Approximately(offsetY, y)) {
			offsetY = y;
//			Debug.Log("offsetY:"+offsetY);

			int from = IndexFromOffset(offsetY);
			int to = IndexFromOffset(offsetY + rcPanel.sizeDelta.y);

			RemoveUnvisibleCells(from, to);
			ShowVisibleCells(from, to);

//			Debug.Log("usedCells="+usedCells.Count);
//			Debug.Log("unUsedCells="+unUsedCells.Count);
		}
	}

}


public interface UITableViewDelegate {
	float HeightForIndex(UITableView tableview, int index);
}

public interface UITableViewDataSource {
	uint NumberOfCells(UITableView tableview);
	UITableViewCell CellForIndex(UITableView tableview, int index);
}