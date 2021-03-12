using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISpeed : MonoBehaviour
{
    Text text;
    CustomCharacterController character;

    void Start()
    {
        text = GetComponent<Text>();
        character = GetComponentInParent<CustomCharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        text.text = Mathf.RoundToInt(character.speed).ToString();
    }
}
