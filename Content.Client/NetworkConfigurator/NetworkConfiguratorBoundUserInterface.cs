﻿using Content.Shared.DeviceNetwork;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.NetworkConfigurator;

public sealed class NetworkConfiguratorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private NetworkConfiguratorListMenu? _listMenu;
    private NetworkConfiguratorConfigurationMenu? _configurationMenu;

    private NetworkConfiguratorSystem _netConfig;
    private DeviceListSystem _deviceList;

    public NetworkConfiguratorBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _netConfig = _entityManager.System<NetworkConfiguratorSystem>();
        _deviceList = _entityManager.System<DeviceListSystem>();
    }

    public void OnRemoveButtonPressed(string address)
    {
        SendMessage(new NetworkConfiguratorRemoveDeviceMessage(address));
    }

    protected override void Open()
    {
        base.Open();

        switch (UiKey)
        {
            case NetworkConfiguratorUiKey.List:
                _listMenu = new NetworkConfiguratorListMenu(this);
                _listMenu.OnClose += Close;
                _listMenu.ClearButton.OnPressed += _ => OnClearButtonPressed();
                _listMenu.OpenCentered();
                break;
            case NetworkConfiguratorUiKey.Configure:
                _configurationMenu = new NetworkConfiguratorConfigurationMenu();
                _configurationMenu.OnClose += Close;
                _configurationMenu.Set.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Set);
                _configurationMenu.Add.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Add);
                //_configurationMenu.Edit.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Edit);
                _configurationMenu.Clear.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Clear);
                _configurationMenu.Copy.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Copy);
                _configurationMenu.Show.OnPressed += OnShowPressed;
                _configurationMenu.Show.Pressed = _netConfig.ConfiguredListIsTracked(Owner.Owner);
                _configurationMenu.OpenCentered();
                break;
        }
    }

    private void OnShowPressed(BaseButton.ButtonEventArgs args)
    {
        if (!args.Button.Pressed)
        {
            _netConfig.ToggleVisualization(Owner.Owner, false);
            return;
        }

        if (_entityManager.GetComponent<MetaDataComponent>(Owner.Owner).EntityLifeStage == EntityLifeStage.Initialized)
        {
            // We're in mapping mode. Do something hacky.
            SendMessage(new ManualDeviceListSyncMessage(null, null));
            return;
        }

        _netConfig.ToggleVisualization(Owner.Owner, true);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case NetworkConfiguratorUserInterfaceState configState:
                _listMenu?.UpdateState(configState);
                break;
            case DeviceListUserInterfaceState listState:
                _configurationMenu?.UpdateState(listState);
                break;
        }
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (_configurationMenu == null
            || _entityManager.GetComponent<MetaDataComponent>(Owner.Owner).EntityLifeStage > EntityLifeStage.Initialized
            || message is not ManualDeviceListSyncMessage cast
            || cast.Device == null
            || cast.Devices == null)
        {
            return;
        }

        _netConfig.SetActiveDeviceList(Owner.Owner, cast.Device.Value);
        _deviceList.UpdateDeviceList(cast.Device.Value, cast.Devices);
        _netConfig.ToggleVisualization(Owner.Owner, true);
        _configurationMenu.Show.Pressed = true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _listMenu?.Dispose();
        _configurationMenu?.Dispose();
    }

    private void OnClearButtonPressed()
    {
        SendMessage(new NetworkConfiguratorClearDevicesMessage());
    }

    private void OnConfigButtonPressed(NetworkConfiguratorButtonKey buttonKey)
    {
        SendMessage(new NetworkConfiguratorButtonPressedMessage(buttonKey));
    }
}
