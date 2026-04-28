using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostGenerator : MonoBehaviour
{
    public GameObject Ghost;
    private float countTime = 0;
    public float TimeGeneratorGhost = 1;
    private int countGhost = 0;

    void Update()
    {
        countTime += Time.deltaTime;
        if (countTime >= TimeGeneratorGhost && countGhost < 10)
        {
            GeneratorNewGhost();
            countTime = 0;
            countGhost +=1 ;
        }       
    }
    void GeneratorNewGhost()
    {
        Vector3 creationPoint = positionRandom();
        Instantiate(Ghost, creationPoint, transform.rotation);
    }
    Vector3 positionRandom()
    {
        Vector3 position = Random.insideUnitSphere * 5; //posicao aleatória de uma esfera de raio 5
        position += transform.position; //adiciona a posição do gerador ao valor aleatório
        position.y = transform.position.y; //copia a posição do y de objeto gerador
        return position;
    }
}