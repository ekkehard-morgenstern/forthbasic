
13 constant bc-cr
10 constant bc-lf
27 constant bc-escape
48 constant bc-digit-lo
57 constant bc-digit-hi
0 constant bc-false
-1 constant bc-true
256 constant bc-max-line-size
128 constant bc-max-lines

variable b-window-width
variable b-window-height
variable b-window-size
variable b-window-buffer
variable b-cursor-x
variable b-cursor-y

variable b-old-window-width
variable b-old-window-height
variable b-old-window-size
variable b-old-window-buffer

variable b-quit-flag

: b-save-window-info ( -- )
    \ back up window info
    b-window-width  @ b-old-window-width  !
    b-window-height @ b-old-window-height !
    b-window-size   @ b-old-window-size   !
    b-window-buffer @ b-old-window-buffer ! ;

: b-get-new-window-info ( -- )
    \ get current window dimensions
    form b-window-width ! b-window-height ! 
    b-window-width @ b-window-height @ * b-window-size ! ;

: b-window-info-change? ( -- t )
    \ see if any of the window parameters have changed
    b-window-width  @ b-old-window-width  @ <>
    b-window-height @ b-old-window-height @ <> or ;

: b-window-zero-line ( tgtptr tgtwid -- )
    \ zero out buffer line (no checking)
    0 +do ( tgtptr )
        \ clear target cell
        dup 0 ! 
        \ advance pointer
        cell+   ( tgtptr )
    loop   ( tgtptr )
    drop ;

: b-window-copy-line ( tgtptr srcptr tgtwid srcwid -- )
    \ copy buffer line (no parameter checking!)
    \ compute diffwid = tgtwid - srcwid
    2dup - rot       ( tgtptr srcptr diffwid tgtwid srcwid )
    \ compute minimum width
    min                     ( tgtptr srcptr diffwid minwid )
    \ copy line
    0 +do ( tgtptr srcptr diffwid )
        >r
        \ copy cell
        2dup @ ! ( tgtptr srcptr )
        \ advance pointers
        cell+ swap cell+ swap
        \ prepare for next iteration
        r>
    loop ( tgtptr srcptr diffwid )
    \ check if target line was larger
    dup 0> if ( tgtptr srcptr diffwid )
        nip     ( tgtptr diffwid )
        \ fill remaining target cells with zero
        b-window-zero-line
    else    ( tgtptr srcptr diffwidh )
        drop 2drop
    endif ;
        
: b-cell-addr ( bufaddr x y linew -- addr )
    * + cells + ;

: b-copy-line-old-to-new ( y -- )
    \ copy line from old to new window buffer (no y checking)
    dup ( y y )
    \ compute pointer in new buffer 
    b-window-buffer @ swap ( y newbuf y )
    0 swap                 ( y newbuf 0 y )
    b-window-width @       ( y newbuf 0 y w )
    b-cell-addr            ( y newptr )
    swap                   ( newptr y )
    \ compute pointer in old buffer
    b-old-window-buffer @ swap  ( newptr oldbuf y )
    0 swap                      ( newptr oldbuf 0 y )
    b-old-window-width @        ( newptr oldbuf 0 y w )
    b-cell-addr                 ( newptr oldptr )
    \ prepare for copying line
    b-window-width @            ( newptr oldptr newwidth )
    b-old-window-width @        ( newptr oldptr newwidth oldwidth )
    b-window-copy-line ;

: b-window-copy-old-to-new ( -- )
    \ copy old window buffer to new window buffer
    \ get new and old window height
    b-window-height @ b-old-window-height @     ( newh oldh )
    \ compute difference
    2dup - rot                                  ( diffh newh oldh )
    \ compute minimum between old and new height
    min                                         ( diffh minh )
    \ loop over lines
    0 +do   ( diffh )
        \ copy line
        i b-copy-line-old-to-new
    loop ( diffh )
    \ check if new window height was greater
    dup 0> if ( diffh )
        \ yes: zero out remaining lines
        0 +do
            b-old-window-buffer @ 0 i b-old-window-width @  ( oldbuf 0 y w )
            b-cell-addr     ( oldptr )
            b-old-window-width @ ( oldptr w )
            b-window-zero-line
        loop
    else ( diffh )
        \ no
        drop
    endif ;

: b-window-resize-buffer ( -- )
    \ resize the window buffer (old vs new)
    \ first check if there has been no previous buffer; in this case, do nothing
    \ otherwise, copy the old content into the new buffer
    \ allocate a new buffer
    b-window-size @ cells allocate throw b-window-buffer !
    \ check if an old buffer exists
    b-old-window-buffer @ 0<> if
        \ buffer already existed before size change: copy old content into new buffer
        b-window-copy-old-to-new
        \ free old buffer
        b-old-window-buffer @ free throw
        0 b-old-window-buffer !
    endif ;

: b-update-window-size ( -- )
    \ save previous window parameters
    b-save-window-info
    \ get new text window parameters
    b-get-new-window-info
    \ check if any window parameter has changed
    b-window-info-change? if
        \ yes: resize window buffer
        b-window-resize-buffer
    endif ;

b-update-window-size

: b-update-window-size? ( -- t )
    \ check if window dimensions have changed
    form b-window-width <> b-window-height <> or ;

: b-NEWLINE
    \ output newline character(s)
    bc-cr emit bc-lf emit ;

: b-ESC ( -- )
    \ output escape character
    bc-escape emit ;

: b-CSI ( -- )
    \ output control sequence introducer ( ESC [ )
    b-ESC ." [" ;

: b-SEMIC ( -- ) 
    \ output semicolon
    ." ;" ;

: b-set-autowrap ( -- )
    b-CSI ." ?7h" ;

: b-reset-autowrap ( -- )
    b-CSI ." ?7l" ;

: b-clear ( -- )
    \ clear screen and set cursor to top left screen position (raw)
    b-ESC ." c" ;

: b-handle-refresh ( -- )
    \ refresh entire screen
    \ clear screen, turn off auto-wrap
    b-clear b-reset-autowrap
    \ iterate over lines
    b-window-buffer @ b-window-height @ 0 +do 
        \ ( addr ) set cursor position to beginning of current line
        0 i at-xy
        \ ( addr ) iterate over line, output chars
        b-window-width @ 0 +do dup @ emit cell+ loop
    loop
    \ restore cursor position, re-enable autowrap
    b-cursor-x @ 1- b-cursor-y @ 1- at-xy b-set-autowrap ;

: b-outnum ( n -- )
    \ output decimal number (without surrounding blanks)
    dup 0= if 
        \ if the value is zero, only output that
        drop ." 0"
    else
        \ if the number is negative, output minus sign and negate
        dup 0< if
            ." -"
            negate
        endif
        \ divide number by 10
        dup 10 /
        \ if non-zero, recurse with that number
        dup 0<> if
            recurse
        else
            drop
        endif
        \ output least significant digit of decimal number
        10 mod bc-digit-lo + emit
    endif ;

: b-auto-update-window ( -- )
    \ check if window update is necessary, and do so if so
    b-update-window-size? if b-update-window-size then ;

: b-cursor-y-invalid? ( n -- t )
    \ check if cursor Y position is invalid
    dup 0< over b-window-height >= or nip ;

: b-cursor-x-invalid? ( n -- t )
    \ check if cursor X position is invalid
    dup 0< over b-window-width >= or nip ;

: b-set-cursor ( x y -- )
    b-cursor-y ! b-cursor-x ! ;

: b-not ( n -- ~n )
    invert ;

: b-locate ( x y -- )
    \ set cursor to specified screen position (starting from 1,1)
    b-auto-update-window
    ( x y -- x-1 y-1 )
    1- swap 1- swap
    ( x y -- x y t )
    dup b-cursor-y-invalid?
    ( x y t -- x y t t )
    2 pick b-cursor-x-invalid?
    ( x y t t -- ) 
    or b-not if 2dup 1+ swap 1+ swap b-set-cursor at-xy then ;

: b-cls ( -- )
    \ clear screen and set cursor to top left screen position
    b-clear 1 1 b-locate ;

b-cls

: b-cursor-address ( -- addr )
    \ compute buffer address for cursor position
    b-cursor-y @ 1- b-window-width @ * b-cursor-x @ 1- + cells b-window-buffer @ + ;

: b-handle-page-up ( -- )
    ;

: b-handle-page-down ( -- )
    ;

: b-handle-home ( -- )
    ;

: b-handle-end ( -- )
    ;

: b-handle-insert ( -- )
    ;

: b-handle-delete ( -- )
    ;

: b-handle-f1 ( -- )
    ;

: b-handle-f2 ( -- )
    ;

: b-handle-f3 ( -- )
    ;

: b-handle-f4 ( -- )
    ;

: b-handle-f5 ( -- )
    ;

: b-handle-f6 ( -- )
    ;

: b-handle-f7 ( -- )
    ;

: b-handle-f8 ( -- )
    ;

: b-handle-f9 ( -- )
    ;

: b-handle-f10 ( -- )
    ;

: b-handle-f11 ( -- )
    ;

: b-handle-f12 ( -- )
    \ set quit flag
    bc-true b-quit-flag ! ;

: b-handle-escape ( -- )
    bye ;

: b-scroll-up ( -- )
    b-CSI ." S" ;

: b-scroll-down ( -- )
    b-CSI ." T" ;

: b-anticipate-cursor-up-event ( x y -- x y )
    \ recompute anticipated cursor position as if cursor up had been pressed
    \ sub 1 from y 
    1- 
    \ check if window height has been exceeded
    dup 0<= if
        \ yes: add 1 to y
        1+
        \ scroll down
        b-scroll-down
    endif ;

: b-anticipate-cursor-up ( -- x y )
    \ get remembered cursor position
    b-cursor-x @ b-cursor-y @
    \ recompute anticipated cursor position as if cursor up had been pressed
    b-anticipate-cursor-up-event ;

: b-handle-cursor-up ( -- )
    \ get anticipated cursor position
    b-anticipate-cursor-up ( -- x y )
    \ locate to anticipated position
    b-locate ;

: b-anticipate-cursor-down-event ( x y -- x y )
    \ recompute anticipated cursor position as if cursor down had been pressed
    \ add 1 to y 
    1+ 
    \ check if window height has been exceeded
    dup b-window-height @ > if
        \ yes: sub 1 from y
        1-
        \ scroll up
        b-scroll-up
    endif ;

: b-anticipate-cursor-down ( -- x y )
    \ get remembered cursor position
    b-cursor-x @ b-cursor-y @
    \ recompute anticipated cursor position as if cursor down had been pressed
    b-anticipate-cursor-down-event ;

: b-handle-cursor-down ( -- )
    \ get anticipated cursor position
    b-anticipate-cursor-down ( -- x y )
    \ locate to anticipated position
    b-locate ;

: b-anticipate-return-event ( x y -- x y )
    \ recompute anticipated cursor position as if return had been pressed
    \ reset x to 1
    swap drop 1 swap
    \ move anticipated cursor down
    b-anticipate-cursor-down-event ;

: b-anticipate-return ( -- x y )
    \ get remembered cursor position
    b-cursor-x @ b-cursor-y @
    \ recompute anticipated cursor position as if return had been pressed
    b-anticipate-return-event ;

: b-anticipate-next-char ( -- x y )
    \ get remembered cursor position
    b-cursor-x @ b-cursor-y @
    \ add 1 to x and check if window width has been exceeded
    over 1+ b-window-width @ > if
        \ yes: recompute anticipated cursor position as if return had been pressed
        b-anticipate-return-event
    else
        \ no: add 1 to x for real
        swap 1+ swap
    endif ;

: b-handle-cursor-right ( -- )
    \ cursor right has been pressed
    \ get anticipated cursor position
    b-anticipate-next-char ( -- x y )
    \ locate to anticipated position
    b-locate ;

: b-handle-return ( -- )
    \ return key has been pressed
    \ get anticipated cursor position
    b-anticipate-return ( -- x y )
    \ locate to anticipated position
    b-locate ;

: b-anticipate-cellar-event ( x y -- x y )
    \ recompute anticipated cursor position as if cursor moved backwards
    \ to the right hand edge of the screen
    \ reset x to b-window-width
    swap drop b-window-width @ swap
    \ move anticipated cursor up
    b-anticipate-cursor-up-event ;

: b-anticipate-cellar ( -- x y )
    \ get remembered cursor position
    b-cursor-x @ b-cursor-y @
    \ recompute anticipated cursor position as if cursor appeared at eol
    b-anticipate-cellar-event ;

: b-anticipate-prev-char ( -- x y )
    \ get remembered cursor position
    b-cursor-x @ b-cursor-y @
    \ sub 1 from x and check if window width has been exceeded
    over 1- 0<= if
        \ yes: recompute anticipated cursor position as if cursor appeared at eol
        b-anticipate-cellar-event
    else
        \ no: sub 1 from x for real
        swap 1- swap
    endif ;

: b-handle-cursor-left ( -- )
    \ cursor right has been pressed
    \ get anticipated cursor position
    b-anticipate-prev-char ( -- x y )
    \ locate to anticipated position
    b-locate ;

: b-handle-backspace ( -- )
    \ backspace key has been pressed
    \ get anticipated cursor position
    b-anticipate-prev-char ( -- x y )
    \ locate to anticipated position
    2dup b-locate 
    \ rub out character under cursor
    32 emit
    \ locate to anticipated position again
    b-locate ;

: b-emit ( chr -- )
    \ emit character and store into window buffer
    dup
    \ ( chr chr ) compute buffer position
    b-cursor-address
    \ ( chr chr addr ) store character info into cell
    ! 
    \ compute anticipated cursor position
    b-anticipate-next-char
    \ ( chr x y ) output character for real
    2 pick emit
    \ ( chr x y ) locate to anticipated position
    b-locate
    \ ( chr )
    drop ;

: b-type ( addr u -- )
    \ output 'u' characters from address 'addr'
    0 u+do dup c@ b-emit char+ loop drop ;

s" Forth BASIC v0.1 - Copyright (c) Ekkehard Morgenstern. All rights reserved." b-type
b-handle-return
s" Licensable under the GNU General Public License (GPL) v3 or higher." b-type
b-handle-return
s" Written for use with GNU Forth (aka GForth)." b-type
b-handle-return
b-handle-return

: b-input-handler ( -- )
    key? if
        ekey ekey>char if ( c ) 
            case
                12      of b-handle-refresh endof
                13      of b-handle-return endof
                27      of b-handle-escape endof
                127     of b-handle-backspace endof
                ( c ) \ default handling:
                dup 32 < over 126 > or if
                else
                    \ printable character: first check if window size has changed
                    b-auto-update-window
                    \ compute anticipated cursor position
                    b-anticipate-next-char ( -- x y )
                    \ output character
                    2 pick b-emit
                    \ locate to anticipated position
                    b-locate
                endif
            endcase

        else ekey>fkey if ( key-id )
            case
                \ cursor keys
                k-up    of b-handle-cursor-up       endof
                k-down  of b-handle-cursor-down     endof
                k-left  of b-handle-cursor-left     endof
                k-right of b-handle-cursor-right    endof
                \ pageup / pagedown
                k-prior of b-handle-page-up         endof
                k-next  of b-handle-page-down       endof
                \ home / end
                k-home  of b-handle-home            endof
                k-end   of b-handle-end             endof
                \ insert / delete
                k-insert of b-handle-insert         endof
                k-delete of b-handle-delete         endof
                \ function keys
                k-f1    of b-handle-f1              endof
                k-f2    of b-handle-f2              endof
                k-f3    of b-handle-f3              endof
                k-f4    of b-handle-f4              endof
                k-f5    of b-handle-f5              endof
                k-f6    of b-handle-f6              endof
                k-f7    of b-handle-f7              endof
                k-f8    of b-handle-f8              endof
                k-f9    of b-handle-f9              endof
                k-f10   of b-handle-f10             endof
                k-f11   of b-handle-f11             endof
                k-f12   of b-handle-f12             endof
            endcase
        else ( keyboard-event )
            drop

        then then
    then ;

: b-screen-editor ( -- )
    \ BASIC screen editor
    begin 
        b-input-handler 
    b-quit-flag @ until ;

b-screen-editor

