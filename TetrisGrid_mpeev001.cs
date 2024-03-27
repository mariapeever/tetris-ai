using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetrisGrid_mpeev001 : MonoBehaviour
{
    public GameObject prefab;
    public int Instances;
    public int Width;
    public int Height;
    // Start is called before the first frame update
    private void Start()
    {
        
        int N = (int) Mathf.Ceil(Mathf.Sqrt(this.Instances-1));
        int H = (int) Mathf.Ceil(((N * Width + N * Height) / 2) / Width);
        int V = (int) Mathf.Ceil(((N * Width + N * Height) / 2) / Height);
        
        int I = this.Instances;

        this.transform.position = new Vector3(0, 0);
        prefab.transform.position = new Vector3(0, 0);
        
        for (int i = 0; i < H; i++)
        {
            for(int j = 0; j < V; j++)
            {
                int idx = i * V + j;
                if ((i > 0 || j > 0) && idx < I)
                {
                    GameObject Instance = Instantiate(prefab, new Vector3(i * Width, -j * Height), Quaternion.identity);
                    Instance.name = $"{prefab.name} (Clone) ({idx})";
                }
            }
        }

        prefab.name = $"{prefab.name} (0)";
    }
}
