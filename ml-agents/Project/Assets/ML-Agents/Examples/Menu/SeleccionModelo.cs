using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SeleccionModelo : MonoBehaviour
{
    private int currentSceneIndex = -1;

    public void cambiarNivel(int numeroNivel)
    {
        if (currentSceneIndex != -1)
        {
            SceneManager.UnloadSceneAsync(currentSceneIndex);
        }

        SceneManager.LoadScene(numeroNivel);
        currentSceneIndex = numeroNivel;
    }
}
