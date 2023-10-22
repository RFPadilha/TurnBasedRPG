using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float totalFadeTime = .5f;
    private float fadeTimer = .5f;
    private float fadeSpeed = 3f;
    private Color textColor;
    private float moveYSpeed = 10f;
    private float scaleAmount = 1f;
    public static DamagePopup Create(Vector3 position, int damage, Color color)
    {
        Transform damagePopupTransform = Instantiate(Resources.Load<Transform>("Prefabs/DamagePopup"), position, Quaternion.identity);
        DamagePopup damagePopup = damagePopupTransform.GetComponent<DamagePopup>();
        damagePopup.SetUp(damage, color);
        return damagePopup;
    }
    private void Awake()
    {
        textMesh = transform.GetComponent<TextMeshPro>();
    }
    public void SetUp(int value, Color color)
    {
        textColor = color;
        textMesh.color = textColor;
        textMesh.text = value.ToString();
        transform.LookAt(transform.position + Camera.main.transform.forward);//facing camera
    }
    private void Update()
    {
        transform.position += new Vector3(0, moveYSpeed) * Time.deltaTime;
        fadeTimer -= Time.deltaTime;

        if(fadeTimer > totalFadeTime * .5f)
        {
            //increases text size on first half of text lifetime
            transform.localScale += Vector3.one * scaleAmount * Time.deltaTime;
        }
        else
        {
            //decreases size on second half
            transform.localScale -= Vector3.one * scaleAmount * Time.deltaTime;
        }

        if (fadeTimer <= 0)
        {
            textColor.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = textColor;
            if(textMesh.color.a <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
