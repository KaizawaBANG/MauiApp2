// PDF Export functionality using jsPDF
window.pdfExport = {
    exportTableToPdf: function (tableId, title, filename) {
        // Check if jsPDF is already loaded (UMD build exposes it as window.jspdf)
        let jsPDF;
        if (typeof window.jspdf !== 'undefined' && window.jspdf.jsPDF) {
            jsPDF = window.jspdf.jsPDF;
        } else if (typeof window.jspdf !== 'undefined') {
            // Try direct access
            jsPDF = window.jspdf;
        } else {
            console.error('jsPDF library not loaded. Please ensure it is included in index.html');
            alert('PDF export failed: jsPDF library not loaded. Please refresh the page and try again.');
            return;
        }

        try {
            const doc = new jsPDF('l', 'mm', 'a4'); // Landscape for tables
            
            const table = document.getElementById(tableId);
            if (!table) {
                console.error('Table not found:', tableId);
                alert('Table not found. Please ensure the report table is visible.');
                return;
            }

            // Add title
            doc.setFontSize(16);
            doc.text(title, 145, 15, { align: 'center' });
            
            // Add date
            const now = new Date();
            doc.setFontSize(10);
            doc.text(`Generated: ${now.toLocaleString()}`, 145, 22, { align: 'center' });
            
            // Get table rows
            const rows = [];
            const headerRow = table.querySelector('thead tr');
            if (headerRow) {
                const headers = Array.from(headerRow.querySelectorAll('th')).map(th => th.textContent.trim());
                rows.push(headers);
            }
            
            const bodyRows = table.querySelectorAll('tbody tr');
            bodyRows.forEach(row => {
                const cells = Array.from(row.querySelectorAll('td')).map(td => {
                    // Get text content, handling nested elements
                    const text = td.textContent.trim();
                    return text;
                });
                rows.push(cells);
            });

            // Add footer rows if exists
            const footerRow = table.querySelector('tfoot tr');
            if (footerRow) {
                const footerCells = Array.from(footerRow.querySelectorAll('td')).map(td => td.textContent.trim());
                rows.push(footerCells);
            }
            
            if (rows.length === 0) {
                alert('No data to export.');
                return;
            }
            
            // Calculate column widths dynamically based on content
            const colCount = rows.length > 0 ? rows[0].length : 0;
            if (colCount === 0) {
                alert('No columns found in table.');
                return;
            }
            
            const availableWidth = 270; // A4 landscape width minus margins
            const colWidth = availableWidth / colCount;
            const fontSize = 9;
            
            let y = 35;
            const pageHeight = 190;
            
            rows.forEach((row, index) => {
                // Check if we need a new page
                if (y > pageHeight && index > 0) {
                    doc.addPage();
                    y = 20;
                }

                let x = 15;
                row.forEach((cell, cellIndex) => {
                    doc.setFontSize(index === 0 || index === rows.length - 1 ? fontSize + 1 : fontSize);
                    doc.setFont('helvetica', (index === 0 || index === rows.length - 1) ? 'bold' : 'normal');
                    
                    // Word wrap for long text
                    const maxWidth = colWidth - 2;
                    const lines = doc.splitTextToSize(cell || '', maxWidth);
                    const lineHeight = 5;
                    lines.forEach((line, lineIndex) => {
                        doc.text(line, x, y + (lineIndex * lineHeight), { maxWidth: maxWidth });
                    });
                    x += colWidth;
                });
                
                // Increase y position based on content height
                const maxLines = Math.max(...row.map(cell => {
                    const maxWidth = colWidth - 2;
                    const lines = doc.splitTextToSize(cell || '', maxWidth);
                    return lines.length;
                }));
                const cellHeight = (maxLines * 5) + 2;
                y += cellHeight;
            });
            
            doc.save(filename || 'report.pdf');
        } catch (error) {
            console.error('Error generating PDF:', error);
            alert('Error generating PDF: ' + error.message);
        }
    },

    exportSalesSummaryToPdf: function (tableId, reportNumber, issueDate, startDate, endDate, groupBy, filename) {
        // Check if jsPDF is already loaded
        let jsPDF;
        if (typeof window.jspdf !== 'undefined' && window.jspdf.jsPDF) {
            jsPDF = window.jspdf.jsPDF;
        } else if (typeof window.jspdf !== 'undefined') {
            jsPDF = window.jspdf;
        } else {
            console.error('jsPDF library not loaded.');
            alert('PDF export failed: jsPDF library not loaded. Please refresh the page and try again.');
            return;
        }

        try {
            const doc = new jsPDF('p', 'mm', 'a4'); // Portrait for report
            
            const table = document.getElementById(tableId);
            if (!table) {
                console.error('Table not found:', tableId);
                alert('Table not found. Please ensure the report table is visible.');
                return;
            }

            // Page dimensions with better margins
            const pageWidth = 210;
            const pageHeight = 297;
            const margin = 15;
            const contentWidth = pageWidth - (margin * 2);
            let y = margin;

            // Clean header section
            const headerY = y;
            
            // Logo (top right) - simple
            const logoWidth = 50;
            const logoHeight = 25;
            const logoX = pageWidth - margin - logoWidth;
            const logoY = headerY;
            
            // Try to get logo from existing img element on page, or load it
            let logoAdded = false;
            const existingLogo = document.querySelector('img[src*="quad2"], img.header-logo');
            if (existingLogo && existingLogo.complete) {
                try {
                    const canvas = document.createElement('canvas');
                    canvas.width = existingLogo.naturalWidth || existingLogo.width;
                    canvas.height = existingLogo.naturalHeight || existingLogo.height;
                    const ctx = canvas.getContext('2d');
                    ctx.drawImage(existingLogo, 0, 0);
                    const base64 = canvas.toDataURL('image/png');
                    doc.addImage(base64, 'PNG', logoX, logoY, logoWidth, logoHeight);
                    logoAdded = true;
                } catch (e) {
                    console.log('Could not add logo from existing element:', e);
                }
            }
            
            // If logo not added, try to load it
            if (!logoAdded) {
                const img = new Image();
                img.crossOrigin = 'anonymous';
                try {
                    // Try synchronous approach with XMLHttpRequest
                    const xhr = new XMLHttpRequest();
                    xhr.open('GET', '/images/quad2.png', false); // Synchronous
                    xhr.responseType = 'blob';
                    xhr.send();
                    if (xhr.status === 200) {
                        const blob = xhr.response;
                        const reader = new FileReader();
                        reader.readAsDataURL(blob);
                        // Note: FileReader is async, so we'll use a placeholder for now
                        // In production, you might want to make this function async
                    }
                } catch (e) {
                    console.log('Could not load logo:', e);
                }
            }
            
            // Draw placeholder if logo not added
            if (!logoAdded) {
                // Simple text fallback
                doc.setFontSize(11);
                doc.setFont('helvetica', 'bold');
                doc.setTextColor(0, 0, 0);
                doc.text('QUADTECH', logoX + logoWidth/2, logoY + logoHeight/2, { align: 'center' });
            }
            
            // Report title (top left) - clean and simple
            doc.setFontSize(24);
            doc.setFont('helvetica', 'bold');
            doc.setTextColor(0, 0, 0);
            doc.text('Sales Summary Report', margin, y + 8);
            y += 12;
            
            // Report details - clean and simple
            doc.setFontSize(9);
            doc.setFont('helvetica', 'normal');
            doc.setTextColor(0, 0, 0);
            
            doc.text(`Report #: ${reportNumber}`, margin, y);
            y += 5;
            doc.text(`Generated: ${issueDate}`, margin, y);
            y += 5;
            doc.text(`Period: ${startDate} - ${endDate}`, margin, y);
            y += 5;
            doc.text(`Grouped by: ${groupBy}`, margin, y);
            y += 10;
            
            // Simple divider line
            doc.setDrawColor(200, 200, 200);
            doc.setLineWidth(0.5);
            doc.line(margin, y, pageWidth - margin, y);
            y += 10;

            // Table section
            const tableStartY = y;
            
            // Get table data
            const rows = [];
            // Report table headers
            const reportHeaders = ['#', 'Period', 'Transactions', 'Subtotal', 'Tax Amount', 'Total'];
            rows.push(reportHeaders);
            
            const bodyRows = table.querySelectorAll('tbody tr');
            let rowNumber = 1;
            bodyRows.forEach(row => {
                const cells = Array.from(row.querySelectorAll('td')).map(td => {
                    let text = td.textContent.trim();
                    // Remove currency symbol and strong tags
                    text = text.replace(/₱/g, '').replace(/<strong>/g, '').replace(/<\/strong>/g, '').trim();
                    return text;
                });
                
                // Format as report row: #, Period, Transactions, Subtotal, Tax, Total
                const reportRow = [
                    rowNumber.toString(),
                    cells[0] || '', // Period
                    cells[1] || '', // Transactions
                    cells[2] || '', // Subtotal
                    cells[3] || '', // Tax Amount
                    cells[4] || ''  // Total Amount
                ];
                rows.push(reportRow);
                rowNumber++;
            });

            if (rows.length === 0) {
                alert('No data to export.');
                return;
            }

            // Table column widths (adjusted for 6 columns)
            const colWidths = [12, 50, 25, 30, 30, 33]; // #, Period, Transactions, Subtotal, Tax, Total
            const colAligns = ['center', 'left', 'right', 'right', 'right', 'right'];
            
            // Draw table header - clean and simple
            let x = margin;
            doc.setFontSize(9);
            doc.setFont('helvetica', 'bold');
            doc.setTextColor(0, 0, 0);
            
            rows[0].forEach((header, index) => {
                const width = colWidths[index];
                // Draw text
                doc.text(header, x + (colAligns[index] === 'center' ? width/2 : colAligns[index] === 'right' ? width - 2 : 2), y + 5, { 
                    align: colAligns[index] 
                });
                x += width;
            });
            
            // Simple underline for header
            doc.setDrawColor(0, 0, 0);
            doc.setLineWidth(0.5);
            doc.line(margin, y + 6, margin + contentWidth, y + 6);
            
            y += 8;

            // Draw table rows with better styling
            doc.setFont('helvetica', 'normal');
            doc.setFontSize(8);
            const maxY = pageHeight - margin - 20; // Leave space for footer
            
            for (let i = 1; i < rows.length; i++) {
                // Check if we need a new page
                if (y > maxY) {
                    // Add footer to current page
                    doc.setFontSize(8);
                    doc.setTextColor(150, 150, 150);
                    doc.text('Page ' + (doc.internal.pages.length - 1), pageWidth / 2, pageHeight - 10, { align: 'center' });
                    doc.setTextColor(0, 0, 0);
                    
                    doc.addPage();
                    y = margin + 10;
                    
                    // Redraw header on new page
                    x = margin;
                    doc.setFontSize(9);
                    doc.setFont('helvetica', 'bold');
                    doc.setTextColor(0, 0, 0);
                    
                    rows[0].forEach((header, index) => {
                        const width = colWidths[index];
                        doc.text(header, x + (colAligns[index] === 'center' ? width/2 : colAligns[index] === 'right' ? width - 2 : 2), y + 5, { 
                            align: colAligns[index] 
                        });
                        x += width;
                    });
                    
                    doc.setDrawColor(0, 0, 0);
                    doc.setLineWidth(0.5);
                    doc.line(margin, y + 6, margin + contentWidth, y + 6);
                    
                    y += 8;
                    doc.setFont('helvetica', 'normal');
                    doc.setFontSize(8);
                }

                const row = rows[i];
                x = margin;
                
                // Set font for data rows
                doc.setFontSize(8);
                doc.setFont('helvetica', 'normal');
                doc.setTextColor(0, 0, 0);
                
                row.forEach((cell, index) => {
                    const width = colWidths[index];
                    
                    // Draw text
                    const cellText = cell || '';
                    const textX = colAligns[index] === 'center' ? x + width/2 : 
                                 colAligns[index] === 'right' ? x + width - 2 : x + 2;
                    
                    // Make numbers bold for better visibility
                    if (index >= 2 && !isNaN(parseFloat(cellText.replace(/[₱,]/g, '')))) {
                        doc.setFont('helvetica', 'bold');
                    } else {
                        doc.setFont('helvetica', 'normal');
                    }
                    
                    doc.text(cellText, textX, y + 5, { 
                        align: colAligns[index],
                        maxWidth: width - 4
                    });
                    x += width;
                });
                
                // Simple row separator
                doc.setDrawColor(240, 240, 240);
                doc.setLineWidth(0.3);
                doc.line(margin, y + 6, margin + contentWidth, y + 6);
                
                doc.setTextColor(0, 0, 0);
                y += 7;
            }

            // Summary totals section (if data exists) - clean and simple
            if (rows.length > 1) {
                y += 8;
                // Simple divider line before summary
                doc.setDrawColor(200, 200, 200);
                doc.setLineWidth(0.5);
                doc.line(margin, y, pageWidth - margin, y);
                y += 10;
                
                // Calculate totals from all rows (excluding header)
                let totalTransactions = 0;
                let totalSubtotal = 0;
                let totalTax = 0;
                let totalAmount = 0;
                
                for (let i = 1; i < rows.length; i++) {
                    const row = rows[i];
                    totalTransactions += parseFloat(row[2] || 0);
                    totalSubtotal += parseFloat(row[3] || 0);
                    totalTax += parseFloat(row[4] || 0);
                    totalAmount += parseFloat(row[5] || 0);
                }
                
                // Format numbers
                const formatNumber = (num) => {
                    return isNaN(num) ? '0.00' : num.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
                };
                
                // Summary section - simple
                doc.setFontSize(10);
                doc.setFont('helvetica', 'bold');
                doc.setTextColor(0, 0, 0);
                doc.text('Summary', margin, y);
                y += 8;
                
                doc.setFont('helvetica', 'normal');
                doc.setFontSize(9);
                const summaryItems = [
                    ['Total Transactions:', formatNumber(totalTransactions)],
                    ['Total Subtotal:', '₱' + formatNumber(totalSubtotal)],
                    ['Total Tax:', '₱' + formatNumber(totalTax)],
                    ['Grand Total:', '₱' + formatNumber(totalAmount)]
                ];
                
                summaryItems.forEach((item, index) => {
                    const isLast = index === summaryItems.length - 1;
                    
                    if (isLast) {
                        // Simple line above grand total
                        doc.setDrawColor(0, 0, 0);
                        doc.setLineWidth(0.5);
                        doc.line(margin, y - 2, pageWidth - margin, y - 2);
                        doc.setFont('helvetica', 'bold');
                        doc.setFontSize(10);
                    } else {
                        doc.setFont('helvetica', 'normal');
                        doc.setFontSize(9);
                    }
                    
                    doc.text(item[0], margin, y);
                    doc.text(item[1], pageWidth - margin, y, { align: 'right' });
                    y += 6;
                });
            }
            
            // Simple Footer
            const footerY = pageHeight - 12;
            // Simple footer divider
            doc.setDrawColor(200, 200, 200);
            doc.setLineWidth(0.3);
            doc.line(margin, footerY, pageWidth - margin, footerY);
            
            doc.setFontSize(8);
            doc.setFont('helvetica', 'normal');
            doc.setTextColor(150, 150, 150);
            doc.text('Page ' + doc.internal.pages.length, pageWidth / 2, footerY + 5, { align: 'center' });
            doc.setTextColor(0, 0, 0);
            
            doc.save(filename || 'SalesSummary_Report.pdf');
        } catch (error) {
            console.error('Error generating PDF:', error);
            alert('Error generating PDF: ' + error.message);
        }
    }
};
