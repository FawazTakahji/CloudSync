<lane orientation="vertical"
      horizontal-content-alignment="middle">
    <banner background-border-thickness="48,0"
            padding="12"
            text={#ui.local-saves-view.title}
            background={@Mods/StardewUI/Sprites/BannerBackground}/>
    <frame margin="0,16,0,0"
           padding="32,24"
           background={@Mods/StardewUI/Sprites/ControlBorder}>
        <scrollable peeking="128">
            <lane orientation="vertical">
                <lane *repeat={:Saves}
                      orientation="vertical">
                    <lane orientation="horizontal"
                          vertical-content-alignment="middle">
                        <label text={:DisplayName} />
                        <spacer layout="stretch 0px" />
                        <button text={#ui.local-saves-view.buttons.upload}
                                click=|^UploadSave(Info)| />
                        <spacer layout="20px 0px" />
                        <button text={#ui.local-saves-view.buttons.info}
                                click=|^ShowInfo(Info)| />
                    </lane>
                    <image layout="stretch 4px"
                           margin="8, 16"
                           fit="stretch"
                           tint="#C66E04"
                           sprite={@Mods/StardewUI/Sprites/ThinHorizontalDividerUncolored} />
                </lane>
            </lane>
        </scrollable>
    </frame>
</lane>