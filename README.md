<p>
<br>
<div align="center">

# Al final le ponemos nombre

Proyecto de entrenamiento de modelos de IA con aprendizaje por refuerzo (reinforcement learning) en Unity. Corresponde a el trabajo práctico grupal de la materia INTELIGENCIA ARTIFICIAL (9525) de la Facultad de Ingeniería de la Universidad de Buenos Aires.

<img src="https://pbs.twimg.com/media/EiyTLiuXYAYyvcO.jpg"/>

</p>
</br>

---

<p>
<br>
<div align="left">

## Introducción
El aprendizaje por refuerzo es una técnica de enseñanza que implica recompensar los comportamientos positivos y castigar los negativos. 
Consta de un aprendizaje empírico, por lo que el agente informático está en constante búsqueda de aquellas decisiones que le premien y a la par evita aquellos caminos que, por experiencia propia, son penalizados.
  
<div align="center">
<img title="a title" alt="Alt text" src="/ml-agents/docs/images/ciclo.JPG">
</div>

- Agente: La entidad que aprende y toma decisiones.

- Entorno: El contexto en el que el agente interactúa y recibe retroalimentación.

<div align="center">
<img title="a title" alt="Alt text" src="/ml-agents/docs/images/voley2.JPG">

<img title="a title" alt="Alt text" src="/ml-agents/docs/images/basquet.JPG">
</div>

- Observaciones: Los distintos elementos que componen el entorno. Corresponde a la capa de entrada de la red neuronal.

<div align="center">
<img title="a title" alt="Alt text" src="/ml-agents/docs/images/voley.JPG">
<img title="a title" alt="Alt text" src="/ml-agents/docs/images/observaciones.JPG">
</div>

- Acciones: Las opciones que el agente puede tomar en respuesta a las observaciones del entorno. Corresponde a la capa de salida de la red neuronal.

<div align="center">
<img title="a title" alt="Alt text" src="/ml-agents/docs/images/basquet2.JPG">
</div>

- Recompensas: La retroalimentación positiva o negativa que el agente recibe por sus acciones.

<div align="center">
<img title="a title" alt="Alt text" src="/ml-agents/docs/images/voley3.JPG">
</div>
  
---

<p>
<br>
<div align="left">

## Dependencias

- Python (3.8.13 o superior)

Para desarrollar el trabajo utilizamos [**ML-Agents**](https://unity-technologies.github.io/ml-agents/), un framework de aprendizaje por refuerzo desarrollado por [Unity Technologies](https://store.unity.com/download) que permite a los desarrolladores de juegos y otros entornos de simulación entrenar agentes de inteligencia artificial (IA) en entornos virtuales.
  
```
$ python -m pip install mlagents==0.30.0
```

- Unity (2021.3 o posterior)
- Paquete de Unity com.unity.ml-agents
- Paquete de Unity com.unity.ml-agents.extensions

<div align="center">
<img title="a title" alt="Alt text" src="/ml-agents/docs/images/image-banner.png">
</div>


Para la visualización del entrenamiento a lo largo del tiempo usamos **TensorBoard**, el kit de herramientas desarrollado por TensorFlow. Dentro de la aplicación se pueden analizar las estadísticas de entrenamiento como también el cambio de la política de los modelos a lo largo del tiempo. 

```bash
$ pip install tensorboard
```

<div align="center">
<img src="https://www.tensorflow.org/static/site-assets/images/project-logos/tensorboard-logo-social.png" width="600" height="337"/>
</div>

PyTorch es una biblioteca open source para realizar cómputos usando data flow graphs, la forma fundamental de representar modelos de aprendizaje profundo. Además facilita el entrenamiento y aprendizaje en CPU y GPU.

```bash
$ pip3 install torch~=1.7.1 -f https://download.pytorch.org/whl/torch_stable.html
```

<div align="center">
<img src="https://149695847.v2.pressablecdn.com/wp-content/uploads/2020/02/Pytorch.png"/>
</div>

## Entrenamiento

ML-Agents usa una técnica de entrenamiento por refuerzo llamada **PPO** (Optimización de políticas próximas) es una técnica que utiliza una red neuronal para aproximar la función ideal que asigna las observaciones de un agente a la mejor acción que un agente puede realizar en un estado determinado.

Para configurar el entrenamiento se usa un archivo de formato YAML con el cual podemos alterar los valores iniciales de la política del modelo. Entre otras, se contienen estas variables:

| Variable | Descripción |
| ----------- | ----------- |
| **entropía** | Medida de incertidumbre. Esto corresponde a cuán aleatorias son las decisiones de un agente.|
| **beta** | Corresponde a la fuerza de la regularización de la entropía, lo que hace que la política sea "más aleatoria". Esto asegura que los agentes exploren adecuadamente el espacio de acción durante el entrenamiento.|
| **gamma** | Factor de descuento para recompensas futuras. Esto se puede considerar como qué tan lejos en el futuro el agente debería preocuparse por las posibles recompensas. En situaciones en las que el agente debería estar actuando en el presente para prepararse para las recompensas en un futuro lejano, este valor debería ser grande. En los casos en que las recompensas son más inmediatas, puede ser menor.|
| **epsilon** | Umbral aceptable de divergencia entre la política antigua y la nueva durante la actualización del gradiente descendente. Establecer este valor en un valor pequeño dará como resultado actualizaciones más estables, pero también ralentizará el proceso de capacitación.
| **buffer_size** | Cuántas experiencias (observaciones de agentes, acciones y recompensas obtenidas) se deben recopilar antes de realizar cualquier aprendizaje o actualización del modelo. Un valor muy alto puede perjudicar el entrenamiento |
| **batch_size** | Número de experiencias utilizadas para una iteración de una actualización de descenso de gradiente. Esto siempre debe ser una fracción del **buffer_size** |
| **learning_rate** | Fuerza de cada paso de actualización de descenso de gradiente.|
| **num_layers** | Cuántas capas ocultas están presentes después de la entrada de observación.|
| **hidden_units** | Cuántas unidades hay en cada capa completamente conectada de la red neuronal.|
| **max_steps** | Cuantos pasos de la simulación durará el entrenamiento. Para problemas más complejos se debería subir el número"

Un ejemplo de archivo:

    behaviors:
      Walker:
        trainer_type: ppo
        hyperparameters:
          batch_size: 2048 // 
          buffer_size: 20480
          learning_rate: 0.0003
          beta: 0.005
          epsilon: 0.2
          lambd: 0.95
          num_epoch: 3
          learning_rate_schedule: linear
        network_settings:
          normalize: true
          hidden_units: 512
          num_layers: 3
          vis_encode_type: simple
        reward_signals:
          extrinsic:
            gamma: 0.995
            strength: 1.0
        keep_checkpoints: 5
        max_steps: 30000000
        time_horizon: 1000
        summary_freq: 30000

Para iniciar una sesión de entrenamiento, solo basta tener abierto en Unity la escena con el agente que se busca entrenar y correr:

    mlagents-learn <path al archivo de configuración> --run-id= <id único del modelo de red neuronal>

Se puede usar los siguientes flags:

- --resume : Reanudar una sesión de entrenamiento para un id dado.
- --force : Sobreescribir un id.
- --initialize-from= : Comenzar una sesión de entrenamiento para un nuevo id a partir de un modelo preentrenado.

Para correr TensorBoard y visualizar el progreso del modelo como también la actualización de la política del modelo correr:

```bash
$ tensorboard --logdir results
```

## Bibliografía

[Reinforcement Learning](https://huggingface.co/tasks/reinforcement-learning)

[Example Learning Environments](https://github.com/Unity-Technologies/ml-agents/blob/develop/docs/Learning-Environment-Examples.md)

[Unity Installation & Set-up](https://github.com/miyamotok0105/unity-ml-agents/blob/master/docs/Installation.md)

[Training with Proximal Policy Optimization](https://github.com/miyamotok0105/unity-ml-agents/blob/master/docs/Training-PPO.md)

[Training Configuration File](https://github.com/Unity-Technologies/ml-agents/blob/develop/docs/Training-Configuration-File.md)

[Training intelligent adversaries using self-play with ML-Agents](https://blog.unity.com/technology/training-intelligent-adversaries-using-self-play-with-ml-agents)

[Training In Cooperative Multi-Agent Environments with MA-POCA](https://github.com/Unity-Technologies/ml-agents/blob/develop/docs/ML-Agents-Overview.md#training-in-cooperative-multi-agent-environments-with-ma-poca)

[Using TensorBoard to Observe Training](https://github.com/Unity-Technologies/ml-agents/blob/develop/docs/Using-Tensorboard.md#using-tensorboard-to-observe-training)

## Autores

<div align="center">
  
| Realizado por:                                                      |
| ------------------------------------------------------------------- |
| [Manuel Diéguez](https://github.com/jmdieguez)                      |
| [Tomás Della Vecchia](https://github.com/tomdv18)                   |
| [Santiago Marczewski](https://github.com/smarczewski)               |
| [Ignacio Montecalvo](https://github.com/imontecalvo)                |

  </div>
