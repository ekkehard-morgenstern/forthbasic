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

\ === VARIABLES =========================================================================

\ window dimensions, in character cells
variable b-window-width
variable b-window-height
variable b-window-size

\ window buffer, composed of cells
\ each cell contains 16 bit of information
\ .
\       +------------+----------+
\       | mm bbb fff | cccccccc |
\       +------------+----------+
\ .
\       m - mode value:
\               0 - regular
\               1 - bold or bright
\               2 - blink (if supported)
\               3 - reverse
\       b - background color 0..7
\       f - foreground color 0..7
\       c - character value (ASCII 7 bit + bit 8)
\           (UTF-8 not supported)

variable b-window-buffer

\ text cursor position (1,1) = upper left corner
variable b-cursor-x
variable b-cursor-y

\ line mark indicator
variable b-mark-flag

\ line mark Y position and count
variable b-mark-y
variable b-mark-cnt

\ text colors and attribute: current (for new characters typed)
variable b-attribute

\ text colors and attribute: default (used for empty cells)
variable b-default-attribute

\ text colors and attribute: mark (used for marking cells)
variable b-mark-attribute

\ text colors and attribute: backup (during screen refresh)
variable b-attribute-backup

\ text colors and attribute: user-defined (backup during high-level refresh)
variable b-attribute-user

\ window dimensions and buffer backup during window resizing
variable b-old-window-width
variable b-old-window-height
variable b-old-window-size
variable b-old-window-buffer

\ quit flag for screen editor (if set, drops back into FORTH)
variable b-quit-flag

\ copy of line being executed, as characters. unused if 0
variable b-line-copy
variable b-line-size

