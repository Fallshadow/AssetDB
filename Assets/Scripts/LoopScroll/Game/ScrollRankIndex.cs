using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScrollRankIndex : MonoBehaviour 
{
    public Image backgroundImage;
    public TextMeshProUGUI indexTmp;
    public TextMeshProUGUI nameTmp;
    public TextMeshProUGUI scoreTmp;


    void ScrollCellIndex (int idx) 
    {
		string name = "Rank " + idx.ToString ();
        indexTmp.text = (idx + 1).ToString();
        gameObject.name = name;
	}
}
