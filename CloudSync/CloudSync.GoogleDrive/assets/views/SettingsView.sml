<lane orientation="vertical"
      horizontal-content-alignment="middle"
      layout="70%">
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
                      layout="stretch content"
                      horizontal-content-alignment="start">
                    <panel *switch={IsLoggedIn}>
                        <button text={#ui.buttons.login}
                                *case="false"
                                click=|Login()| />
                        <button text={#ui.buttons.logout}
                                *case="true"
                                click=|Logout()| />
                    </panel>

                    <label text={#ui.settings-view.timeout}
                           tooltip={#ui.settings-view.timeout-tooltip}
                           margin="0,16,0,8" />
                    <textinput tooltip={#ui.settings-view.timeout-tooltip}
                               layout="stretch 54px"
                               text={<>TimeoutText} />

                    <expander layout="stretch content"
                              header-padding="0,12"
                              header-background-tint="#99c"
                              margin="0,0,0,4"
                              tooltip={#ui.settings-view.advanced-settings-warning} >
                        <label *outlet="header" text={#ui.settings-view.advanced-settings-title} />

                        <lane orientation="vertical"
                              layout="stretch content"
                              horizontal-content-alignment="start">
                            <label text={#ui.settings-view.client-id-title}
                                    tooltip={#ui.settings-view.client-id-tooltip} />
                            <textinput layout="stretch 54px"
                                       margin="0,16,0,16"
                                       tooltip={#ui.settings-view.client-id-tooltip}
                                       text={<>ClientId} />

                            <label text={#ui.settings-view.client-secret-title}
                                    tooltip={#ui.settings-view.client-secret-tooltip} />
                            <textinput layout="stretch 54px"
                                       margin="0,16,0,16"
                                       tooltip={#ui.settings-view.client-secret-tooltip}
                                       text={<>ClientSecret} />

                            <label text={#ui.settings-view.refresh-token-title}
                                   tooltip={#ui.settings-view.refresh-token-tooltip} />
                            <textinput layout="stretch 54px"
                                       margin="0,16,0,16"
                                       tooltip={#ui.settings-view.refresh-token-tooltip}
                                       text={<>RefreshToken} />
                        </lane>
                    </expander>
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