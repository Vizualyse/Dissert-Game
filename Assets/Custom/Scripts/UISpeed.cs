using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISpeed : MonoBehaviour
{
    Text text;
    public GameObject player;

    public int smoothingFactor = 10;
    int counter = 0;
    Vector3 lastPos;
    Vector3 pos;

    void Start()
    {
        text = GetComponent<Text>();
        lastPos = player.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        pos = player.transform.position;
        Vector3 moved = pos - lastPos;
        moved = new Vector3(moved.x, 0, moved.z);
        lastPos = pos;
        float speed = moved.magnitude / Time.deltaTime;

        if (counter == smoothingFactor)
        {
            text.text = Mathf.RoundToInt(speed).ToString();
            counter = 0;
        }
        else
            counter++;
    }
}
