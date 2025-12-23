<lane orientation="vertical" horizontal-content-alignment="middle">
    <banner background-border-thickness="48,0"
            padding="12"
            text={#ui.settings-view.banner-title}
            background={@Mods/StardewUI/Sprites/BannerBackground}/>
    <frame margin="0,16,0,0"
           padding="32,24"
           background={@Mods/StardewUI/Sprites/ControlBorder}>
        <lane orientation="vertical">
            <scrollable peeking="128">
                <lane orientation="vertical"
                      horizontal-content-alignment="start">
                    <lane orientation="horizontal">
                        <checkbox label-text={#ui.settings-view.setting.auto-upload}
                                  tooltip={#ui.settings-view.setting.auto-upload.tooltip}
                                  is-checked={<>AutoUpload}
                                  layout="content 32px" />

                        <checkbox label-text={#ui.settings-view.setting.backup-saves}
                                  tooltip={#ui.settings-view.setting.backup-saves.tooltip}
                                  is-checked={<>BackupSaves}
                                  layout="content 32px"
                                  margin="16,0" />

                        <checkbox label-text={#ui.settings-view.setting.purge-backups}
                                  tooltip={#ui.settings-view.setting.purge-backups.tooltip}
                                  is-checked={<>PurgeBackups}
                                  layout="content 32px" />
                    </lane>

                    <label text={#ui.settings-view.setting.backups-to-keep}
                           tooltip={#ui.settings-view.setting.backups-to-keep.tooltip}
                           margin="0,16,0,8" />
                    <slider min="1"
                            max="10"
                            interval="1"
                            tooltip={#ui.settings-view.setting.backups-to-keep.tooltip}
                            value={<>BackupsToKeep} />

                    <image layout="stretch 4px"
                           margin="8, 16"
                           fit="stretch"
                           tint="#C66E04"
                           sprite={@Mods/StardewUI/Sprites/ThinHorizontalDividerUncolored} />

                    <label text={#ui.settings-view.setting.provider-extension} />
                    <lane orientation="horizontal"
                          vertical-content-alignment="middle">
                        <dropdown options={:Extensions}
                                  option-format={:ExtensionFormat}
                                  selected-option={<>SelectedExtension}
                                  option-min-width="350" />
                        <spacer layout="stretch 0px" />
                        <panel *switch={IsExtensionSettingsVisible}>
                            <button *case="true"
                                    text={#ui.settings-view.buttons.extension-settings}
                                    click=|OpenExtensionSettings()| />
                            <button *case="false"
                                    text={#ui.settings-view.buttons.extension-settings}
                                    opacity="0.5" />
                        </panel>
                    </lane>

                    <image layout="stretch 4px"
                           margin="8, 16"
                           fit="stretch"
                           tint="#C66E04"
                           sprite={@Mods/StardewUI/Sprites/ThinHorizontalDividerUncolored} />

                    <label *if={:GCSInstalled}
                           text={#ui.settings-view.gcs-installed}
                           margin="0,0,0,8" />

                    <checkbox label-text={#ui.settings-view.setting.overwrite-save-settings}
                              tooltip={#ui.settings-view.setting.overwrite-save-settings.tooltip}
                              layout="content 32px"
                              is-checked={<>OverwriteSaveSettings}
                              opacity={:Opacity} />

                    <label text={#ui.settings-view.setting.ui-scale}
                           margin="0,16,0,0"
                           opacity={:Opacity} />
                    <slider min="75"
                            max="150"
                            interval="5"
                            value={<>UiScale}
                            opacity={:Opacity} />

                    <label text={#ui.settings-view.setting.zoom-level}
                           margin="0,16,0,0"
                           opacity={:Opacity} />
                    <slider min="75"
                            max="200"
                            interval="5"
                            value={<>ZoomLevel}
                            opacity={:Opacity} />

                    <label text={#ui.settings-view.setting.slingshot-fire-mode}
                           margin="0,16,0,8"
                           opacity={:Opacity} />
                    <dropdown options={:SlingshotFireModes}
                              option-format={:SlingshotFireModeFormat}
                              selected-option={<>UseLegacySlingshotFiring}
                              option-min-width="300"
                              opacity={:Opacity} />

                    <checkbox label-text={#ui.settings-view.setting.controller-placement-tile-indicator}
                              is-checked={<>ShowPlacementTileForGamepad}
                              margin="0,16,0,0"
                              layout="content 32px"
                              opacity={:Opacity} />

                    <checkbox label-text={#ui.settings-view.setting.controller-rumble}
                              is-checked={<>Rumble}
                              margin="0,16,0,0"
                              layout="content 32px"
                              opacity={:Opacity} />
                </lane>
            </scrollable>

            <spacer layout="0px 16px" />

            <lane orientation="horizontal">
                <button text={#ui.buttons.reset}
                        click=|Reset()| />
                <spacer layout="stretch 0px" />

                <button text={#ui.buttons.cancel}
                        click=|Cancel()| />
                <spacer layout="20px 0px" />

                <button text={#ui.buttons.save}
                        click=|Save()| />
            </lane>
        </lane>
    </frame>
</lane>