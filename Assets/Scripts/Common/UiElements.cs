using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// I gesti che si fanno su una schermata: trovare un elemento, scrivergli
/// dentro, accendergli una classe, agganciargli un click.
///
/// Sta accanto a <see cref="UiFormat"/> e per la stessa ragione: non è di
/// nessuna schermata in particolare. <c>UiFormat</c> decide <b>come si scrive</b>
/// un numero, questo decide <b>come si tocca</b> un elemento.
///
/// <b>La rete sta qui, in fondo.</b> Se l'elemento non c'è — nome sbagliato,
/// UXML cambiato — il metodo non fa niente e non esplode. Per questo chi chiama
/// non ha più bisogno di un <c>if (x != null)</c> a ogni riga: la domanda è
/// sempre la stessa, e si risponde una volta sola.
/// </summary>
public static class UiElements
{
    // ---------- RICERCA ----------

    /// <summary>La Label con questo nome, o <c>null</c> se non c'è.</summary>
    public static Label GetLabel(this VisualElement root, string name)
    {
        return root?.Q<Label>(name);
    }

    /// <summary>Il Button con questo nome, o <c>null</c> se non c'è.</summary>
    public static Button GetButton(this VisualElement root, string name)
    {
        return root?.Q<Button>(name);
    }

    /// <summary>L'elemento con questo nome, qualunque cosa sia, o <c>null</c> se non c'è.</summary>
    public static VisualElement GetVisualElement(this VisualElement root, string name)
    {
        return root?.Q<VisualElement>(name);
    }

    /// <summary>La prima Label con questa classe. Serve dentro gli elementi che si ripetono, dove il nome non è unico.</summary>
    public static Label GetLabelByClass(this VisualElement root, string className)
    {
        return root?.Q<Label>(className: className);
    }

    /// <summary>
    /// Tutti gli elementi con questa classe, nell'ordine in cui stanno
    /// nell'UXML. È l'UXML a dire quanti sono: qui li si conta, non li si decide.
    /// </summary>
    public static List<VisualElement> GetVisualElementsByClass(this VisualElement root, string className)
    {
        return root == null
            ? new List<VisualElement>()
            : root.Query<VisualElement>(className: className).ToList();
    }

    // ---------- SCRITTURA ----------

    /// <summary>Scrive in una Label che si ha già in mano.</summary>
    public static void SetText(this Label label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }

    /// <summary>Scrive nella Label con questo nome, senza doverla tenere da parte.</summary>
    public static void SetText(this VisualElement root, string name, string value)
    {
        root.GetLabel(name).SetText(value);
    }

    /// <summary>
    /// Riempie una barra: la larghezza in percentuale del suo contenitore.
    /// Fuori da 0..1 non si va, così un dato storto non sfonda il disegno.
    /// </summary>
    public static void SetFill(this VisualElement fill, float ratio)
    {
        if (fill != null)
        {
            fill.style.width = Length.Percent(Mathf.Clamp01(ratio) * 100f);
        }
    }

    /// <summary>C'è o non c'è. Non è opacità: l'elemento nascosto non occupa nemmeno spazio.</summary>
    public static void SetVisible(this VisualElement element, bool visible)
    {
        if (element != null)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    /// <summary>Accende o spegne una classe su un elemento che si ha già in mano.</summary>
    public static void SetClass(this VisualElement element, string className, bool enabled)
    {
        element?.EnableInClassList(className, enabled);
    }

    /// <summary>Accende o spegne una classe sull'elemento con questo nome.</summary>
    public static void SetClass(this VisualElement root, string name, string className, bool enabled)
    {
        root.GetVisualElement(name).SetClass(className, enabled);
    }

    // ---------- INTERAZIONE ----------

    /// <summary>Aggancia un'azione al Button con questo nome.</summary>
    public static void OnClick(this VisualElement root, string name, Action action)
    {
        Button button = root.GetButton(name);

        if (button != null)
        {
            button.clicked += action;
        }
    }

    /// <summary>
    /// Rende l'elemento e tutto ciò che ha sotto trasparente al mouse: una
    /// schermata su cui non si clicca non deve nemmeno poter intercettare un
    /// click diretto al gioco che sta sotto.
    /// </summary>
    public static void IgnoreClicks(this VisualElement root)
    {
        if (root == null)
        {
            return;
        }

        root.pickingMode = PickingMode.Ignore;
        root.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Ignore);
    }
}
