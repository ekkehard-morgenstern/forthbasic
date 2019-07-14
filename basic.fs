\ Forth BASIC v0.1 - A BASIC interpreter written in FORTH.
\ Copyright (C) 2019 Ekkehard Morgenstern
\ 
\ This program is free software: you can redistribute it and/or modify
\ it under the terms of the GNU General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.
\ 
\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.
\ 
\ You should have received a copy of the GNU General Public License
\ along with this program.  If not, see <https://www.gnu.org/licenses/>.
\ 
\ Programs written in Forth BASIC do not fall under this license except 
\ where otherwise noted.
\ 
\ This version has been written for use with GNU Forth (aka GForth). 
\ Contact info: ekkehard@ekkehardmorgenstern.de

require constants.fs
require variables.fs
require attribmgmt.fs
require controlseq.fs
require windowres.fs
require screenupd.fs
require tokenizer.fs
require execution.fs
require keyboard.fs

\ === MAIN PROGRAM ======================================================================

bc-def-mode bc-def-bgcol bc-def-fgcol b-make-attr dup b-attribute ! b-default-attribute !
bc-mark-mode bc-mark-bgcol bc-mark-fgcol b-make-attr b-mark-attribute ! 0 b-mark-flag !
0 b-line-copy ! 0 b-line-size !
b-init-window
b-cls
s" Forth BASIC v0.1  Copyright (C) 2019  Ekkehard Morgenstern" b-type b-output-return
b-output-return
s" This program comes with ABSOLUTELY NO WARRANTY; for details type 'warranty'." b-type b-output-return
s" This is free software, and you are welcome to redistribute it under certain conditions; type 'conditions' for details." b-type b-output-return
s" Licensable under the GNU General Public License (GPL) v3 or higher, of which you should have received a copy in the file 'LICENSE'." b-type b-output-return
s" Programs created with Forth BASIC do NOT fall under this license unless otherwise noted." b-type b-output-return
s" Written for use with GNU Forth (aka GForth)." b-type b-output-return
b-output-return
s" Type 'help' to enter built-in help system." b-type b-output-return
s" Type 'edit' to enter built-in program editor." b-type b-output-return
s" Hit F12 to enter FORTH, type 'basic' to return." b-type b-output-return
s" Hit ESC to leave." b-type b-output-return
b-output-return
s" Enjoy!" b-type b-output-return
b-output-return
b-handle-refresh
b-mark-line
b-screen-editor

