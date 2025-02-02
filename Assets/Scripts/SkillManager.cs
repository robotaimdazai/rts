using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillManager : MonoBehaviour
{
    public SkillData skill;
    AudioSource _sourceContextualSource;
    GameObject _source;
    Button _button;
    bool _ready;
    
    public void Initialize(SkillData skill, GameObject source)
    {
        this.skill = skill;
        _source = source;
        UnitManager um = source.GetComponent<UnitManager>();
        if (um != null)
            _sourceContextualSource = um.contextualSource;
    }
    
    public void Trigger(GameObject target = null)
    {
        if (!_ready) return;
        StartCoroutine(_WrappedTrigger(target));
    }
    
    public void SetButton(Button button)
    {
        _button = button;
        _SetReady(true);
    }
    private IEnumerator _WrappedTrigger(GameObject target)
    {
        if (_sourceContextualSource != null && skill.onStartSound)
            _sourceContextualSource.PlayOneShot(skill.onStartSound);
        yield return new WaitForSeconds(skill.castTime);
        if (_sourceContextualSource != null && skill.onEndSound)
            _sourceContextualSource.PlayOneShot(skill.onEndSound);
        skill.Trigger(_source, target);
        _SetReady(false);
        yield return new WaitForSeconds(skill.cooldown);
        _SetReady(true);
    }
    private void _SetReady(bool ready)
    {
        _ready = ready;
        if (_button != null) _button.interactable = ready;
    }
    
}
