<lane orientation="vertical"
      horizontal-content-alignment="middle">
    <banner background-border-thickness="48,0"
            padding="12"
            text={#ui.backups-view.title}
            background={@Mods/StardewUI/Sprites/BannerBackground}/>
    <frame margin="0,16,0,0"
           padding="32,24"
           background={@Mods/StardewUI/Sprites/ControlBorder}>
        <scrollable peeking="128">
            <lane orientation="vertical">
                <panel layout="stretch content"
                       horizontal-content-alignment="middle"
                       *if={Loaded}>
                    <button text={#ui.backups-view.purge-backups}
                            click=|PurgeBackups()| />
                </panel>
                <image layout="stretch 4px"
                       margin="0,8,0,16"
                       fit="stretch"
                       tint="#C66E04"
                       sprite={@Mods/StardewUI/Sprites/ThinHorizontalDividerUncolored}
                       *if={Loaded} />

                <lane *repeat={Backups}
                      orientation="vertical">
                    <lane orientation="horizontal"
                          vertical-content-alignment="middle">
                        <label text={:DisplayName} />
                        <spacer layout="stretch 0px" />
                        <button layout="content"
                                click=|^DeleteBackup(this)|>
                            <image sprite={@Mods/FawazT.CloudSync/Sprites/Icons:Trash} />
                        </button>
                    </lane>
                    <image layout="stretch 4px"
                           margin="0,16"
                           fit="stretch"
                           tint="#C66E04"
                           sprite={@Mods/StardewUI/Sprites/ThinHorizontalDividerUncolored} />
                </lane>
            </lane>
        </scrollable>
    </frame>
</lane>