using UnityEngine;

[CreateAssetMenu(menuName = "aaaaAAAAAaaaaAAA????")]
public class Resource : ScriptableObject
{
    [Tooltip("Название")]
    public string materialName;
    [Tooltip("ID")]
    public int id;
    [Tooltip("Иконка")]
    public Texture2D icon;
    [Tooltip("Префаб руды")]
    public GameObject orePrefab;
    [Tooltip("Префаб предмета")]
    public GameObject itemPrefab;
    [Tooltip("В сумку влезает")]
    public int maxInBag;

    public void SpawnItem(Vector3 position) {
        Instantiate(itemPrefab, position, Quaternion.identity).name = id+"";
    }
}
