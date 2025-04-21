<frame margin="0,16,0,0"
       padding="32,24"
       background={@Mods/StardewUI/Sprites/ControlBorder}>
    <lane orientation="vertical">
        <label text={:Message} />

        <spacer layout="0px 20px" />

        <lane orientation="horizontal">
            <textinput layout="stretch 54px" text={<>Input} />
            <spacer layout="20px 0px" />
            <button text={#ui.buttons.paste}
                    click=|Paste()| />
        </lane>

        <spacer layout="0px 20px" />

        <lane orientation="horizontal" layout="stretch content">
            <button text={#ui.buttons.cancel}
                    click=|Cancel()| />

            <spacer layout="stretch 0px" />

            <button text={#ui.buttons.ok}
                    click=|Ok()| />
        </lane>
    </lane>
</frame>