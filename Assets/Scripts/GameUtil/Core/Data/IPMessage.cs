using System;
using System.Collections.Generic;
using UnityEngine;

// ���� IP ��Ϣ
[Serializable]
public class IPMessage {
    public List<string> info = new List<string>();
    public string ContinentCode = "";
    public string CountryCode = "";
    public string Country = "";
    public string State = "";
    public string City = "";
    public string Ip = "";

    public void ToData(string json) {
        string JSONToParse = json;
        JsonUtility.FromJsonOverwrite(JSONToParse, this);
    }
}