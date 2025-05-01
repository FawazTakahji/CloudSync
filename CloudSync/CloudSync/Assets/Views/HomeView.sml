<panel>
    <lane orientation="vertical" vertical-content-alignment="middle">
        <banner background-border-thickness="48,0"
                padding="12"
                text="CloudSync"
                background={@Mods/StardewUI/Sprites/BannerBackground} />
            <lane margin="0,20,0,0" layout="stretch content" orientation="vertical" horizontal-content-alignment="middle">
                <button layout="285px 176px"
                        click=|OpenMenu("Local")|>
                    <frame padding="16" border-thickness="4">
                        <lane layout="content"
                              orientation="vertical"
                              horizontal-content-alignment="middle">
                            <image layout="60px 60px"
                                   sprite={@Mods/FawazT.CloudSync/Sprites/Icons:Save} />
                            <banner text={#ui.home-view.local-saves} />
                        </lane>
                    </frame>
                </button>

                <spacer layout="0px 20px" />

                <button layout="285px 176px"
                        click=|OpenMenu("Cloud")|>
                    <frame padding="16" border-thickness="4">
                        <lane layout="content"
                              orientation="vertical"
                              horizontal-content-alignment="middle">
                            <image layout="72px 48px"
                                   sprite={@Mods/FawazT.CloudSync/Sprites/Icons:Cloud} />
                            <banner text={#ui.home-view.cloud-saves} />
                        </lane>
                    </frame>
                </button>

                <spacer layout="0px 20px" />

                <button layout="285px 176px"
                        click=|OpenMenu("Settings")|>
                    <frame padding="16" border-thickness="4">
                        <lane layout="content" orientation="vertical" horizontal-content-alignment="middle">
                            <image sprite={@Item/(O)867} />
                            <banner text={#ui.home-view.settings} />
                        </lane>
                    </frame>
                </button>
            </lane>
    </lane>
</panel>