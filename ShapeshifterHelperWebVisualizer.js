// ==UserScript==
// @name         Shapeshifter Helper - Web Visualizer (Symbols -> Colors)
// @namespace    GreaseMonkey
// @version      1.0
// @description  Replaces Shapeshifter symbols with colored boxes containing numbers on the webpage, so it matches with the Shapeshifter Helper app's visuals. Also has Copy HTML button for convenience.
// @match        *://www.neopets.com/medieval/shapeshifter.phtml*
// @grant        none
//
// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⡈⢯⡉⠓⠦⣄⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀ ____ _     ____  ____  _____ ____  _     _  _____ _____  _____ ____
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠻⣉⠹⠷⠀⠀⠀⠙⢷⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀/ ___\/ \ /|/  _ \/  __\/  __// ___\/ \ /|/ \/    //__ __\/  __//  __\
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⠞⠀⠀⠀⠀⠀⠀⠀⢿⡇⠀⠀⠀⠀⠀⠀⠀⠀|    \| |_||| / \||  \/||  \  |    \| |_||| ||  __\  / \  |  \  |  \/|
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⢈⡇⠀⠀⠀⠀⠀⠀⠀⠀\___ || | ||| |-|||  __/|  /_ \___ || | ||| || |     | |  |  /_ |    /
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠹⠝⠀⠀⠀⠀⠀⣼⠃⠀⠀⠀⠀⠀⠀⠀⠀\____/\_/ \|\_/ \|\_/   \____\\____/\_/ \|\_/\_/     \_/  \____\\_/\_\
//⠀⠀⠀⠀⠀⠀⠀⣠⠞⠀⣀⣠⣤⣤⠄⠀⠀⢠⡏⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠚⠢⠼⠿⠟⢛⣾⠃⠀⠀⠀⢸⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀ _     _____ _     ____  _____ ____
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⡴⣻⠃⠀⠀⠀⠀⢸⡉⠀⠀⠀⠀⠀⠀⠀⠀⠀/ \ /|/  __// \   /  __\/  __//  __\
//⠀⠀⠀⠀⠀⠀⠀⠀⣰⢻⡷⠁⠀⠀⠀⠀⠀⢸⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀| |_|||  \  | |   |  \/||  \  |  \/|
//⠀⠀⠀⠀⠀⠀⠀⢰⢽⡟⠁⠀⠀⠀⠀⠀⠀⠀⣇⠀⠀⠀⠀⠀⠀⠀⠀⠀| | |||  /_ | |_/\|  __/|  /_ |    /
//⠀⠀⠀⠀⠀⠀⠀⢾⣿⠀⠀⠀⠀⠀⠀⠀⠀⠀⣸⡆⠀⠀⠀⠀⠀⠀⠀⠀\_/ \|\____\\____/\_/   \____\\_/\_\
//⠀⠀⠀⠀⠀⠀⠀⢸⣿⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⡀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠘⢧⣳⡀⠀⠀⠀⠀⠀⠀⠀⠀⠘⣷⠀⠀⠀⠀⠀⠀⠀      Web Visualizer Version 1.0
//⠀⠀⠀⠀⠀⠀⠀⠀⠈⣷⣱⡀⠀⠀⠀⠀⣸⠀⠀⠀⠈⢻⣦⠀⠀⠀⠀⠀     
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⣷⡙⣆⠀⠀⣾⠃⠀⠀⠀⠀⠈⢽⡆⠀⠀⠀⠀      Script created by @willnjohnson
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠸⡇⢷⡏⠃⢠⠇⠀⠀⣀⠄⠀⠀⠀⣿⡖⠀⠀⠀      
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⡇⢨⠇⠀⡼⢀⠔⠊⠀⠀⠀⠀⠀⠘⣯⣄⢀⠀      (To be used alongside the Shapeshifter Helper C# app.)
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢰⡇⣼⡀⣰⣷⠁⠀⠀⠀⠀⠀⠀⠀⠀⣇⢻⣧⡄      
//⠀⠀⠀⠀⠀⠀⣀⣮⣿⣿⣿⣯⡭⢉⠟⠛⠳⢤⣄⣀⣀⣀⣀⡴⢠⠨⢻⣿      
//⠀   ⢀⣾⣿⣿⣿⣿⢏⠓⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⢨⣿      
//   ⣰⣿⣿⣿⣿⣿⣿⡱⠌⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⢭⣾⠏      
//  ⣰⡿⠟⠋⠛⢿⣿⣿⣊⠡⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣠⣼⡿⠋⠀
// ⠋⠁⠀⠀⠀⠀⠈⠑⠿⢶⣄⣀⣀⣀⣀⣀⣄⣤⡶⠿⠟⠋⠁⠀⠀⠀
//
// ==/UserScript==

(function () {
    'use strict';

    const colorMap = {
        0: 'rgb(46, 139, 87)',   // SeaGreen
        1: 'rgb(178, 34, 34)',   // Firebrick
        2: 'rgb(255, 140, 0)',   // DarkOrange
        3: 'rgb(70, 130, 180)',  // SteelBlue
        4: 'rgb(139, 0, 139)'    // DarkMagenta
    };

    // Helper to normalize filename by removing trailing _number before .gif
    function normalizeFilename(filename) {
        return filename.replace(/_\d+(?=\.gif$)/, '');
    }

    // Step 1: Find all symbols from the board (used to assign mappings)
    const sourceTables = document.querySelectorAll('table[border="1"][bordercolor="gray"]');
    const sourceImages = [];

    for (const table of sourceTables) {
        sourceImages.push(...Array.from(table.querySelectorAll('img')));
    }

    const symImages = sourceImages.filter(img => {
        const src = img.getAttribute('src');
        return src && src.endsWith('.gif') && !src.includes('arrow.gif');
    });

    let N = symImages.length - 2;
    if (N < 0) {
        console.log("Not enough symbols to map.");
        return;
    }

    const mapping = {};
    let assigned = 0;

    for (const img of symImages) {
        let filename = img.src.split('/').pop();
        filename = normalizeFilename(filename);
        if (!(filename in mapping)) {
            mapping[filename] = N - assigned;
            console.log(`${mapping[filename]}: ${filename}`);
            assigned++;
        }
    }

    // Step 2: Extract shape mask from first <table border="0" cellpadding="15" cellspacing="0" width="50" height="50">
    function extractShapeMask() {
        const shapeTable = document.querySelector('table[border="0"][cellpadding="15"][cellspacing="0"][width="50"][height="50"]');
        if (!shapeTable) return [];

        const innerTable = shapeTable.querySelector('table');
        if (!innerTable) return [];

        const mask = [];
        for (const row of innerTable.rows) {
            const rowPattern = [];
            for (const cell of row.cells) {
                const hasImg = cell.querySelector('img') !== null;
                rowPattern.push(hasImg ? 1 : 0);
            }
            mask.push(rowPattern);
        }
        console.log('Extracted shape mask:', mask);
        return mask;
    }

    const shapeMask = extractShapeMask();
    const maskHeight = shapeMask.length;
    const maskWidth = maskHeight > 0 ? shapeMask[0].length : 0;

    // Step 3: Apply mapping of styles across ALL images in both sets of tables
    const allTargetTables = [
        ...document.querySelectorAll('table[border="1"][bordercolor="gray"]'),
        ...document.querySelectorAll('table[align="center"][cellpadding="0"][cellspacing="0"][border="0"]')
    ];

    // To help revert colors after hover, store original styles per cell
    const originalStyles = new WeakMap();

    for (const table of allTargetTables) {
        const images = table.querySelectorAll('img');
        for (const img of images) {
            const src = img.getAttribute('src');
            if (!src || !src.endsWith('.gif') || src.includes('arrow.gif')) continue;

            let filename = src.split('/').pop();
            filename = normalizeFilename(filename);

            const num = mapping[filename];
            if (num === undefined) continue;

            // Create replacement box
            const div = document.createElement('div');
            div.textContent = num;
            div.style.width = img.width + 'px';
            div.style.height = img.height + 'px';
            div.style.display = 'flex';
            div.style.alignItems = 'center';
            div.style.justifyContent = 'center';
            div.style.color = 'white';
            div.style.fontWeight = 'bold';
            div.style.border = '1px solid gray';
            div.style.backgroundColor = colorMap[num % 5];
            div.style.fontFamily = 'Arial, sans-serif';
            div.style.fontSize = '16px';
            div.style.boxSizing = 'border-box';

            img.style.display = 'none';
            img.parentNode.insertBefore(div, img);
        }
    }

    // Step 4: Hover effect on main board table cells
    const boardTable = document.querySelector('table[align="center"][cellpadding="0"][cellspacing="0"][border="0"]');
    if (!boardTable) return;

    const boardRows = Array.from(boardTable.rows);
    const boardHeight = boardRows.length;
    const boardWidth = boardHeight > 0 ? boardRows[0].cells.length : 0;

    function getDivInCell(row, col) {
        if (row < 0 || row >= boardHeight) return null;
        const cells = boardRows[row].cells;
        if (!cells || col < 0 || col >= cells.length) return null;
        return cells[col].querySelector('div');
    }

    function saveOriginalStyles(div) {
        if (!originalStyles.has(div)) {
            originalStyles.set(div, {
                bg: div.style.backgroundColor,
                fg: div.style.color
            });
        }
    }

    for (let r = 0; r < boardHeight; r++) {
        for (let c = 0; c < boardWidth; c++) {
            const cellDiv = getDivInCell(r, c);
            if (!cellDiv) continue;

            cellDiv.style.cursor = 'pointer';

            cellDiv.addEventListener('mouseenter', () => {
                if (r + maskHeight > boardHeight || c + maskWidth > boardWidth) return;

                for (let dy = 0; dy < maskHeight; dy++) {
                    for (let dx = 0; dx < maskWidth; dx++) {
                        if (shapeMask[dy][dx] === 1) {
                            const targetDiv = getDivInCell(r + dy, c + dx);
                            if (!targetDiv) continue;
                            saveOriginalStyles(targetDiv);
                            targetDiv.style.backgroundColor = 'white';
                            targetDiv.style.color = 'black';
                        }
                    }
                }
            });

            cellDiv.addEventListener('mouseleave', () => {
                if (r + maskHeight > boardHeight || c + maskWidth > boardWidth) return;

                for (let dy = 0; dy < maskHeight; dy++) {
                    for (let dx = 0; dx < maskWidth; dx++) {
                        if (shapeMask[dy][dx] === 1) {
                            const targetDiv = getDivInCell(r + dy, c + dx);
                            if (!targetDiv) continue;
                            const orig = originalStyles.get(targetDiv);
                            if (orig) {
                                targetDiv.style.backgroundColor = orig.bg;
                                targetDiv.style.color = orig.fg;
                            }
                        }
                    }
                }
            });
        }
    }

    // Display a Copy HTML button (for convenience, so user doesn't have to open console to copy HTML)
    const contentTd = document.querySelector('td.content');
    if (contentTd) {
        // Ensure contentTd is position: relative for absolute positioning of button
        if (getComputedStyle(contentTd).position === 'static') {
            contentTd.style.position = 'relative';
        }

        const copyBtn = document.createElement('button');
        copyBtn.textContent = 'Copy HTML';
        Object.assign(copyBtn.style, {
            position: 'absolute',
            top: '10px',
            right: '10px',
            backgroundColor: '#007FFF', // Azure blue
            color: 'white',
            border: 'none',
            borderRadius: '8px',
            padding: '8px 16px',
            fontWeight: 'bold',
            cursor: 'pointer',
            zIndex: 10000,
            boxShadow: '0 2px 5px rgba(0,0,0,0.3)',
            userSelect: 'none',
        });
      
        copyBtn.addEventListener('mouseenter', () => Object.assign(copyBtn.style, { backgroundColor: '#003FBF' }));

        copyBtn.addEventListener('mouseleave', () => Object.assign(copyBtn.style, { backgroundColor: '#007FFF' }));
      
        copyBtn.addEventListener('click', () => {
            try {
                const htmlToCopy = contentTd.innerHTML;
                navigator.clipboard.writeText(htmlToCopy).then(() => {
                    copyBtn.textContent = 'Copied!';
                    setTimeout(() => (copyBtn.textContent = 'Copy HTML'), 2000);
                }, () => {
                    alert('Failed to copy HTML.');
                });
            } catch (e) {
                alert('Clipboard API not supported.');
            }
        });

        contentTd.appendChild(copyBtn);
    }
})();
