using UnityEngine;
using System.Collections;

public class TestTableView : MonoBehaviour, UITableViewDelegate, UITableViewDataSource {
	public GameObject cellPrefab;

	void Awake() {
		UITableView tableview = GetComponent<UITableView>();
		tableview.Delegate = this;
		tableview.Datasource = this;

	}

	void UITableViewDelegate.OnScrollChanged(Vector2 pos) {

	}

	float UITableViewDelegate.HeightForIndex(UITableView tableview, int index) {
		return 55;
	}

	uint UITableViewDataSource.NumberOfCells(UITableView tableview) {
		return 50;
	}

	UITableViewCell UITableViewDataSource.CellForIndex(UITableView tableview, int index) {
		UITableViewCell cell = tableview.DequeueReusableCell();
		if (null == cell) {
			GameObject obj = (GameObject)Instantiate(cellPrefab);
			cell = obj.GetComponent<UITableViewCell>();
		}

		cell.index = index; //only required for testing

		TestTableViewCell testCell = (TestTableViewCell)cell;
		testCell.UpdateData();

		return cell;
	}

}
