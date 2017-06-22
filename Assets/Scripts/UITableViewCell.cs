using UnityEngine;
using System.Collections;

public class UITableViewCell : MonoBehaviour {

	[HideInInspector]
	public int index = -1;
	[HideInInspector]
	public string identifier;
	[HideInInspector]
	public RectTransform rcTransform;

	void Awake() {
		rcTransform = GetComponent<RectTransform>();
	}
}
