﻿using System;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngineX;

public class ItemSlot : GamePresentationBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button _itemSlotButton;

    public Image Background;
    public Image ItemIcon;
    public Color HoverBackgroundColor = Color.white;

    [SerializeField] private TextMeshProUGUI _stackText;

    private Color _startBackgroundColor;
    private ItemVisualInfo _currentItem;
    private Action _onItemLeftClicked; // index of item in list, not used here
    private Action _onItemRightClicked; // index of item in list, not used here

    private bool _mouseInside = false;

    private Entity _itemsOwner;
    private bool _init;

    private void InitIfNeeded()
    {
        if (_init)
            return;
        _init = true;

        _startBackgroundColor = Background.color;
        _itemSlotButton.onClick.AddListener(ItemSlotClicked);
    }

    protected override void OnGamePresentationUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1) && _currentItem != null && _mouseInside)
        {
            SecondaryUseItemSlot();
        }
    }

    public virtual void UpdateCurrentItemSlot(ItemVisualInfo item, Action onItemLeftClicked, Action onItemRightClicked, Entity owner, int stacks = -1)
    {
        InitIfNeeded();

        _currentItem = item;
        _onItemLeftClicked = onItemLeftClicked;
        _onItemRightClicked = onItemRightClicked;
        _itemsOwner = owner;

        if (stacks <= 0)
        {
            _stackText.gameObject.SetActive(false);
        }
        else
        {
            _stackText.text = "x" + stacks;
            _stackText.gameObject.SetActive(true);
        }

        if (_currentItem != null)
        {
            ItemIcon.color = ItemIcon.color.ChangedAlpha(1);
            ItemIcon.sprite = _currentItem.Icon;
        }
        else
        {
            ItemIcon.color = ItemIcon.color.ChangedAlpha(0);
            Background.color = _startBackgroundColor;
        }
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        _mouseInside = true;

        if (_currentItem != null)
        {
            Background.color = Color.white;
            TooltipDisplay.Instance.ActivateToolTipDisplay(_currentItem, _itemsOwner);
        }
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        _mouseInside = false;

        Background.color = _startBackgroundColor;
        TooltipDisplay.Instance.DeactivateToolTipDisplay();
    }

    public void ItemSlotClicked()
    {
        PrimaryUseItemSlot();
    }

    public virtual void PrimaryUseItemSlot()
    {
        _onItemLeftClicked?.Invoke();
    }

    public virtual void SecondaryUseItemSlot()
    {
        _onItemRightClicked?.Invoke();
    }

    public ItemVisualInfo GetItemInfoInSlot()
    {
        return _currentItem;
    }
}