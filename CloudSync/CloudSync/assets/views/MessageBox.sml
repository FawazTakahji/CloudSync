<frame margin="0,16,0,0"
       padding="32,24"
       background={@Mods/StardewUI/Sprites/ControlBorder}>
    <lane orientation="vertical">
        <scrollable layout="50%">
            <label text={:Message}
                   focusable="true" />
        </scrollable>

        <panel *switch={:Buttons}>
            <spacer layout="0px 20px"
                   *case="Ok" />
            <spacer layout="0px 20px"
                   *case="OkCancel" />
            <spacer layout="0px 20px"
                   *case="YesNo" />
            <spacer layout="0px 20px"
                   *case="YesNoCancel" />
        </panel>

        <lane *switch={:Buttons} orientation="horizontal" layout="stretch content" horizontal-content-alignment="end">
            <button text={#ui.buttons.cancel}
                    *case="OkCancel"
                    click=|Cancel()| />
            <button text={#ui.buttons.cancel}
                    *case="YesNoCancel"
                    click=|Cancel()| />
            <spacer layout="stretch 0px"
                    *case="OkCancel" />
            <spacer layout="stretch 0px"
                    *case="YesNoCancel" />

            <button text={#ui.buttons.no}
                    *case="YesNo"
                    click=|No()| />

            <spacer layout="20px 0px"
                    *case="YesNo" />

            <button text={#ui.buttons.yes}
                    *case="YesNo"
                    click=|Yes()| />

            <button text={#ui.buttons.no}
                    *case="YesNoCancel"
                    click=|No()| />

            <spacer layout="20px 0px"
                    *case="YesNoCancel" />

            <button text={#ui.buttons.yes}
                    *case="YesNoCancel"
                    click=|Yes()| />

            <button text={#ui.buttons.ok}
                    *case="Ok"
                    click=|Ok()| />
            <button text={#ui.buttons.ok}
                    *case="OkCancel"
                    click=|Ok()| />
        </lane>
    </lane>
</frame>