<panel>
    <lane orientation="vertical"
          vertical-content-alignment="middle"
          horizontal-content-alignment="middle">
        <banner background-border-thickness="48,0"
                padding="12"
                text="CloudSync"
                background={@Mods/StardewUI/Sprites/BannerBackground} />
            <grid margin="0,20,0,0"
                  layout="590px 372px"
                  item-layout="count: 2"
                  item-spacing="20,20">
                <button layout="285px 176px"
                        click=|OpenMenu("Local")|>
                    <frame padding="16"
                           border-thickness="4">
                        <lane layout="content"
                              orientation="vertical"
                              horizontal-content-alignment="middle">
                            <image layout="60px 60px"
                                   sprite={@Mods/FawazT.CloudSync/Sprites/Icons:Save} />
                            <banner text={#ui.home-view.local-saves} />
                        </lane>
                    </frame>
                </button>

                <button layout="285px 176px"
                        click=|OpenMenu("Cloud")|>
                    <frame padding="16"
                           border-thickness="4">
                        <lane layout="content"
                              orientation="vertical"
                              horizontal-content-alignment="middle">
                            <image layout="72px 48px"
                                   sprite={@Mods/FawazT.CloudSync/Sprites/Icons:Cloud} />
                            <banner text={#ui.home-view.cloud-saves} />
                        </lane>
                    </frame>
                </button>

                <button layout="285px 176px"
                        click=|OpenMenu("Backups")|>
                    <frame padding="16"
                           border-thickness="4">
                        <lane layout="content"
                              orientation="vertical"
                              horizontal-content-alignment="middle">
                            <image layout="60px 48px"
                                   sprite={@Mods/FawazT.CloudSync/Sprites/Icons:Archive} />
                            <banner text={#ui.home-view.backups} />
                        </lane>
                    </frame>
                </button>

                <button layout="285px 176px"
                        click=|OpenMenu("Settings")|>
                    <frame padding="16"
                           border-thickness="4">
                        <lane layout="content"
                              orientation="vertical"
                              horizontal-content-alignment="middle">
                            <image sprite={@Item/(O)867} />
                            <banner text={#ui.home-view.settings} />
                        </lane>
                    </frame>
                </button>
            </grid>
    </lane>
</panel>