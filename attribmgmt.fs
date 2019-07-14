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

\ === ATTRIBUTE MANAGEMENT ==============================================================

: >b-attr-fg-col ( col -- attr )
    \ convert color to attribute bits
    bc-fg-vmsk and bc-fg-vshf lshift ;

: >b-attr-bg-col ( col -- attr )
    \ convert color to attribute bits
    bc-bg-vmsk and bc-bg-vshf lshift ;

: >b-attr-mode   ( mode -- attr )
    \ convert mode to attribute bits
    bc-mode-vmsk and bc-mode-vshf lshift ;

: b-attr-fg-col>    ( attr -- col )
    \ convert attribute bits to color
    bc-fg-vshf rshift bc-fg-vmsk and ;

: b-attr-bg-col>    ( attr -- col )
    \ convert attribute bits to color
    bc-bg-vshf rshift bc-bg-vmsk and ;

: b-attr-mode>      ( attr -- mode )
    \ convert attribute bits to mode
    bc-mode-vshf rshift bc-mode-vmsk and ;

: b-make-attr       ( mode bgcol fgcol -- attr )
    \ make combined attribute bits from color/mode info
    >b-attr-fg-col -rot ( fgattr mode bgcol )
    >b-attr-bg-col -rot ( bgattr fgattr mode )
    >b-attr-mode        ( bgattr fgattr modeattr )
    or or ;             ( attr )

: b-split-attr      ( attr -- mode bgcol fgcol )
    \ get separate color/mode info from combined attribute bits
    dup b-attr-mode> swap    ( mode attr )
    dup b-attr-bg-col> swap  ( mode bgcol attr )
    b-attr-fg-col> ;         ( mode bgcol fgcol )

: b-attr-fg-col+    ( attr -- attr )
    b-split-attr    ( mode bgcol fgcol )
    1+
    b-make-attr ;   ( attr )

: b-attr-bg-col+    ( attr -- attr )
    b-split-attr    ( mode bgcol fgcol )
    swap 1+ swap
    b-make-attr ;   ( attr )

: b-attr-mode+      ( attr -- attr )
    b-split-attr    ( mode bgcol fgcol )
    rot 1+ -rot
    b-make-attr ;   ( attr )

