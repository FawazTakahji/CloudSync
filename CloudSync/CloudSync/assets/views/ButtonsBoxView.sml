<frame margin="0,16,0,0"
       padding="32,24"
       background={@Mods/StardewUI/Sprites/ControlBorder}>
    <lane orientation="vertical">
        <scrollable layout="50%">
            <label text={:Message}
                   focusable="true" />
        </scrollable>

        <spacer layout="0px 20px" />

        <lane orientation="horizontal"
              layout="stretch content"
              horizontal-content-alignment="end">
            <lane *repeat={:Buttons}>
                <button text={:Text}
                        click=|RunAction()|
                        margin="16,0,0,0" />
            </lane>
        </lane>
    </lane>
</frame>