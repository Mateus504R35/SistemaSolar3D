using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Componente auxiliar para adicionar ExplicitModelMatrix a todos os
/// GameObjects filhos que possuem Renderer.
/// Coloque-o temporariamente no objeto raiz SistemaSolar3D e execute
/// o comando pelo menu de contexto do componente.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class ExplicitModelMatrixInstaller : MonoBehaviour
{
    [ContextMenu("Adicionar Model Matrix a todos os Renderers")]
    private void InstallOnAllRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        int addedComponents = 0;

        foreach (Renderer currentRenderer in renderers)
        {
            if (currentRenderer.TryGetComponent<ExplicitModelMatrix>(out _))
            {
                continue;
            }

#if UNITY_EDITOR
            Undo.AddComponent<ExplicitModelMatrix>(currentRenderer.gameObject);
            EditorUtility.SetDirty(currentRenderer.gameObject);
#else
            currentRenderer.gameObject.AddComponent<ExplicitModelMatrix>();
#endif

            addedComponents++;
        }

        Debug.Log(
            $"ExplicitModelMatrix adicionado a {addedComponents} objeto(s). " +
            $"Renderers encontrados: {renderers.Length}.",
            this
        );
    }
}
