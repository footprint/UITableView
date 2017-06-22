using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TestTableViewCell : UITableViewCell {
	public Text nameText;

	public void UpdateData() {
		nameText.text = "index="+index;
		gameObject.name = nameText.text;
	}
}
