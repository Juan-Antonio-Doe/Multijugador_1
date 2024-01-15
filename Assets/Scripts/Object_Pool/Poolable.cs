using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Poolable : MonoBehaviour
{
    public ObjectPool pool;
    public bool autoDisable = false;
    public float timeToDisable = 1f;

    private void OnEnable()
    {
        if(autoDisable == true)
        {
            StartCoroutine(CRT_Disable());
        }
    }

    IEnumerator CRT_Disable()
    {
        yield return new WaitForSeconds(timeToDisable);
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        pool?.AddToPool(this);
    }
}
