﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class dlgStudioSelect
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.OK_Button = New System.Windows.Forms.Button()
        Me.Cancel_Button = New System.Windows.Forms.Button()
        Me.lvStudios = New System.Windows.Forms.ListView()
        Me.ilStudios = New System.Windows.Forms.ImageList(Me.components)
        Me.SuspendLayout()
        '
        'OK_Button
        '
        Me.OK_Button.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.OK_Button.Location = New System.Drawing.Point(292, 280)
        Me.OK_Button.Name = "OK_Button"
        Me.OK_Button.Size = New System.Drawing.Size(67, 23)
        Me.OK_Button.TabIndex = 0
        Me.OK_Button.Text = "OK"
        '
        'Cancel_Button
        '
        Me.Cancel_Button.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Cancel_Button.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.Cancel_Button.Location = New System.Drawing.Point(365, 280)
        Me.Cancel_Button.Name = "Cancel_Button"
        Me.Cancel_Button.Size = New System.Drawing.Size(67, 23)
        Me.Cancel_Button.TabIndex = 1
        Me.Cancel_Button.Text = "Cancel"
        '
        'lvStudios
        '
        Me.lvStudios.AutoArrange = False
        Me.lvStudios.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lvStudios.Font = New System.Drawing.Font("Segoe UI", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(238, Byte))
        Me.lvStudios.FullRowSelect = True
        Me.lvStudios.HideSelection = False
        Me.lvStudios.Location = New System.Drawing.Point(3, 3)
        Me.lvStudios.MultiSelect = False
        Me.lvStudios.Name = "lvStudios"
        Me.lvStudios.Size = New System.Drawing.Size(429, 265)
        Me.lvStudios.SmallImageList = Me.ilStudios
        Me.lvStudios.Sorting = System.Windows.Forms.SortOrder.Ascending
        Me.lvStudios.TabIndex = 2
        Me.lvStudios.UseCompatibleStateImageBehavior = False
        Me.lvStudios.View = System.Windows.Forms.View.SmallIcon
        '
        'ilStudios
        '
        Me.ilStudios.ColorDepth = System.Windows.Forms.ColorDepth.Depth16Bit
        Me.ilStudios.ImageSize = New System.Drawing.Size(64, 44)
        Me.ilStudios.TransparentColor = System.Drawing.Color.Transparent
        '
        'dlgStudioSelect
        '
        Me.AcceptButton = Me.OK_Button
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.CancelButton = Me.Cancel_Button
        Me.ClientSize = New System.Drawing.Size(435, 315)
        Me.Controls.Add(Me.OK_Button)
        Me.Controls.Add(Me.lvStudios)
        Me.Controls.Add(Me.Cancel_Button)
        Me.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "dlgStudioSelect"
        Me.ShowInTaskbar = False
        Me.Text = "Select Studio"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents OK_Button As System.Windows.Forms.Button
    Friend WithEvents Cancel_Button As System.Windows.Forms.Button
    Friend WithEvents lvStudios As System.Windows.Forms.ListView
    Friend WithEvents ilStudios As System.Windows.Forms.ImageList

End Class
