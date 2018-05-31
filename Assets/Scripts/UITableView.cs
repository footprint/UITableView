using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public delegate void OnTableViewCell(UITableViewCell cell);

public class UITableView : MonoBehaviour {
	bool isFirstReload = true;
	const float threshold = 0.1f;
	//direction
//	public bool vertical = true;
//	public bool horizontal = false; //not supported yet!

	public UITableViewDelegate Delegate {get;set;}
	public UITableViewDataSource Datasource {get;set;}

	public event OnTableViewCell eventCellWillAppear;
	public event OnTableViewCell eventCellWillDisappear;

	float offsetY = 0;

	int minIndex = 0;
	int maxIndex = 0;

	ScrollRect scrollRect;
	RectTransform rcPanel;
	RectTransform rcContent;

	List<UITableViewCell> usedCells = new List<UITableViewCell>();
	List<UITableViewCell> unUsedCells = new List<UITableViewCell>();

	public int MinIndex {
		get {
			return this.minIndex;
		}
	}

	public int MaxIndex {
		get {
			return this.maxIndex;
		}
	}

	public RectTransform RectTransform {
		get {
			return this.rcPanel;
		}
	}

	public ScrollRect ScrollRect {
		get {
			return this.scrollRect;
		}
	}

	void Awake() {
		rcPanel = GetComponent<RectTransform>();
		scrollRect = GetComponent<ScrollRect>();
		rcContent = scrollRect.content;

		scrollRect.onValueChanged.AddListener(this.OnScrollChanged);
	}

	void OnDestroy() {
		for (int i = 0; i < unUsedCells.Count; ++i) {
			if (null != unUsedCells[i]) {
				Destroy(unUsedCells[i].gameObject);
			}
		}
		unUsedCells.Clear();

		this.eventCellWillAppear = null;
		this.eventCellWillDisappear = null;
	}

	void Start() {
		ReloadData();
		isFirstReload = false;
	}

	void ResetData() {
		offsetY = 0;
	}

	int IndexFromOffset(float offset, ref Vector2 pos) {
		float h = 0;
		pos.y = 0;
		uint num = this.Datasource.NumberOfCells(this);
		for (int i = 0; i < num; ++i) {
			pos.y = -h;
			h += this.Delegate.HeightForIndex(this, i);

			if (h > offset) {
				return i;
			}
		}
		pos.y = -h;
		if (num > 0) {
			return ((int)(num - 1));
		}
		return -1; //out of range
	}

	void InsertCellAtIndex(UITableViewCell cell, int index) {
		cell.index = index;
		cell.enabled = true;

		if (null != this.eventCellWillAppear) {
			this.eventCellWillAppear(cell);
		}

		cell.transform.SetParent(rcContent.transform, false);

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
		if (null != this.eventCellWillDisappear) {
			this.eventCellWillDisappear(cell);
		}

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

	void ShowVisibleCells(int from, int to, Vector2 pos) {
		if (-1 != from) {
			this.minIndex = from;
		}
		if (-1 != to) {
			this.maxIndex = to;
		}
		//
		if (0 == usedCells.Count && -1 != from && -1 != to) {
			float y = pos.y;
			for (int i = from; i <= to; ++i) {
				UITableViewCell cell = this.Datasource.CellForIndex(this, i);
				if (cell) {
					InsertCellAtIndex(cell, i);
					
					cell.rcTransform.anchoredPosition = new Vector2(0, y);
					
					float h = this.Delegate.HeightForIndex(this, i); //cell.rcTransform.sizeDelta.y;
					y -= h;
				}
			}
			return;
		}

		if (from != -1) {
			UITableViewCell _cell = usedCells[0];
			int begin = _cell.index;
			float y = _cell.rcTransform.anchoredPosition.y;
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
			y -= this.Delegate.HeightForIndex(this, end); //cell.rcTransform.sizeDelta.y;
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
		scrollRect.StopMovement();

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

        // float y = 0;
        // float maxY = rcPanel.sizeDelta.y;
        // Transform tranContent = rcContent.transform;
        // for (int i = 0; i < num; ++i) {
        // 	UITableViewCell cell = this.Datasource.CellForIndex(this, i);
        // 	if (cell) {
        // 		usedCells.Add(cell);
        // 		cell.index = i;
        // 		cell.transform.SetParent(tranContent, false);
        // 		cell.rcTransform.anchoredPosition = new Vector2(0, y);
        // 		cell.enabled = true;

        // 		float h = this.Delegate.HeightForIndex(this, i); //cell.rcTransform.sizeDelta.y;
        // 		y -= h;

        // 		if (-y > maxY || Mathf.Approximately(-y, maxY)) {
        // 			break;
        // 		}
        // 	}else {
        // 		break;
        // 	}
        // }

        float y = rcContent.anchoredPosition.y;
        float maxY = rcContent.sizeDelta.y - rcPanel.sizeDelta.y;

        if (y < threshold)
        {
            y = threshold;
        }
        else if (y > (maxY + threshold))
        {
            y = maxY + threshold;
        }

        if (!Mathf.Approximately(offsetY, y))
        {
            offsetY = y;
            //			Debug.Log("offsetY:"+offsetY);

            Vector2 posTo = Vector2.zero;
            Vector2 posFrom = Vector2.zero;
            int from = IndexFromOffset(offsetY, ref posFrom);
            int to = IndexFromOffset(offsetY + rcPanel.sizeDelta.y, ref posTo);

            RemoveUnvisibleCells(from, to);
            ShowVisibleCells(from, to, posFrom);

            //			Debug.Log("usedCells="+usedCells.Count);
            //			Debug.Log("unUsedCells="+unUsedCells.Count);
        }
	}

	public UITableViewCell HeadCellInSight() {
		if (usedCells.Count > 0) {
			return usedCells[0];
		}
		return null;
	}

	public UITableViewCell TailCellInSight() {
		if (usedCells.Count > 1) {
			return usedCells[usedCells.Count - 1];
		}
		return HeadCellInSight();
	}

	public UITableViewCell CellAtIndex(int index) {
		if (usedCells.Count > 0) {
			UITableViewCell cell = usedCells[0];
			if (index == cell.index) {
				return cell;
			}else if (index < cell.index) {
				return null;
			}
			if (usedCells.Count > 1) {
				cell = usedCells[usedCells.Count - 1];
				if (index == cell.index) {
					return cell;
				}else if (index > cell.index) {
					return null;
				}

				for (int i = 1; i < usedCells.Count - 1; ++i) {
					cell = usedCells[i];
					if (cell.index == index) {
						return cell;
					}
				}
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

	public Vector2 GetOffset() {
		return rcContent.anchoredPosition;
	}

	public void ScrollToOffset(Vector2 offset) {
		Vector2 max = MaxOffset();
		if (offset.x > max.x) {
			offset.x = max.x;
		}
		if (offset.y > max.y) {
			offset.y = max.y;
		}
		rcContent.anchoredPosition = offset;
	}

    public Vector2 MaxOffset()
    {
        if (isFirstReload)
        {
            float x = rcContent.sizeDelta.x - rcPanel.sizeDelta.x;
            float y = 0;
            if (this.Datasource != null && this.Delegate != null)
            {
                uint num = this.Datasource.NumberOfCells(this);
                for (int i = 0; i < num; ++i)
                {
                    y += this.Delegate.HeightForIndex(this, i);
                }
				y -= rcPanel.sizeDelta.y;
            }
			return new Vector2(x, y);
        }
        else
        {
            float x = rcContent.sizeDelta.x - rcPanel.sizeDelta.x;
            float y = rcContent.sizeDelta.y - rcPanel.sizeDelta.y;
            return new Vector2(x, y);
        }
    }

	//scrollrect event
	public void OnScrollChanged(Vector2 pos) {
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

			Vector2 posTo = Vector2.zero;
			Vector2 posFrom = Vector2.zero;
			int from = IndexFromOffset(offsetY, ref posFrom);
			int to = IndexFromOffset(offsetY + rcPanel.sizeDelta.y, ref posTo);

			RemoveUnvisibleCells(from, to);
			ShowVisibleCells(from, to, posFrom);

//			Debug.Log("usedCells="+usedCells.Count);
//			Debug.Log("unUsedCells="+unUsedCells.Count);
		}

		this.Delegate.OnScrollChanged(rcContent.anchoredPosition);
	}

}


public interface UITableViewDelegate {
	//required
	float HeightForIndex(UITableView tableview, int index);
	//optional
	void OnScrollChanged(Vector2 pos);
}

public interface UITableViewDataSource {
	//required
	uint NumberOfCells(UITableView tableview);
	UITableViewCell CellForIndex(UITableView tableview, int index);
}