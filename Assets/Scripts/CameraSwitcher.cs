using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private Transform ballTransform;
    public Camera[] cameras;  // Arreglo de cámaras que deseas alternar

    private bool fPCamera;
    void Start()
    {
        print(cameras[0]);
        print(cameras[1]);
        print(cameras[2]);
        fPCamera = false;
        setCameras();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)){
            fPCamera = !fPCamera;
        }else{
            setCameras();
        }
    }

    void setCameras(){
        if (!fPCamera){
            cameras[2].gameObject.SetActive(true);
            cameras[0].gameObject.SetActive(false);
            cameras[1].gameObject.SetActive(false);
        }else{
            int idx = ballTransform.localPosition.x < 0 ? 0 : 1;
            cameras[0].gameObject.SetActive(false);
            cameras[1].gameObject.SetActive(false);
            cameras[idx].gameObject.SetActive(true);

            // cameras[0].gameObject.SetActive(true);
            // cameras[1].gameObject.SetActive(false);
            // cameras[2].gameObject.SetActive(false);
        }
    }
}


