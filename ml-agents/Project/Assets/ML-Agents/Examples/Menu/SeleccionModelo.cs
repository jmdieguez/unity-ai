using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SeleccionModelo : MonoBehaviour
{
    public void cambiarNivel(int numeroNivel){
        SceneManager.LoadScene(numeroNivel);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
