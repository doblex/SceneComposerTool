using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
    /// <summary>
    /// Funzione ricorsiva per avere tutti i filgi di un transform
    /// </summary>
    /// <param name="parent">transform da cui partire</param>
    /// <returns>ritorna tutti i figli del transform</returns>
    public static List<Transform> GetAllChildren(this Transform parent)
    {
        var children = new List<Transform>();
        CollectChildren(parent, children);
        return children;
    }

    private static void CollectChildren(Transform parent, List<Transform> result)
    {
        foreach (Transform child in parent)
        {
            result.Add(child);
            CollectChildren(child, result);
        }
    }
}

public static class Lib
{
    /// <summary>
    /// utility per ottenere tutti gli oggetti di tipo T  in range rispetto ad un centro
    /// </summary>
    /// <typeparam name="T">Qualsiasi MonoBehaviour</typeparam>
    /// <param name="center">centro della circonferenza in cui cercare</param>
    /// <param name="radius">raggio della circonferenza in cui cercare</param>
    /// <returns>lista di oggetti all'interno della circonferenza</returns>
    public static List<T> GetObjectInRange<T>(Vector3 center, float radius) where T : MonoBehaviour
    {
        List<T> Objs = new List<T>();

        T[] candidates = GameObject.FindObjectsByType<T>();

        foreach (T obj in candidates)
        {
            float distance = Vector3.Distance(obj.transform.position, center);

            if (distance <= radius)
            { 
                Objs.Add(obj);
            }
        }

        return Objs;
    }
}

