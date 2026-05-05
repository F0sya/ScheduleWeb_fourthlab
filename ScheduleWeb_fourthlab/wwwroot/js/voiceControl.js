const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

if (SpeechRecognition) {
    const recognition = new SpeechRecognition();
    recognition.lang = 'uk-UA';
    recognition.continuous = true;
    recognition.interimResults = true;

    const numberMap = {
        'один': '1', 'перший': '1',
        'два': '2', 'другий': '2',
        'три': '3', 'третій': '3',
        'чотири': '4', 'четвертий': '4',
        'п\'ять': '5', 'пʼять': '5', 'п’ять': '5',
        'шість': '6', 'сім': '7', 'вісім': '8', 'дев\'ять': '9', 'десять': '10'
    };

    recognition.onresult = (event) => {
        let transcript = '';
        for (let i = event.resultIndex; i < event.results.length; ++i) {
            transcript += event.results[i][0].transcript;
        }
        
        const feedback = document.getElementById('voice-feedback');
        if (feedback) {
            feedback.innerHTML = 'Слухаю: "' + transcript + '"';
            feedback.style.display = 'block';
        }

        if (event.results[event.results.length - 1].isFinal) {
            let finalTranscript = transcript.trim().toLowerCase();
            
            Object.keys(numberMap).forEach(word => {
                if (finalTranscript.includes(word)) {
                    finalTranscript = finalTranscript.replace(word, numberMap[word]);
                }
            });

            console.log('Final voice command (processed):', finalTranscript);
            handleCommand(finalTranscript);
            
            setTimeout(() => {
                if (feedback) feedback.style.display = 'none';
            }, 3000);
        }
    };

    recognition.onstart = () => updateIndicator(true);
    recognition.onerror = (event) => updateIndicator(false);
    recognition.onend = () => {
        const indicator = document.getElementById('voice-indicator');
        if (indicator && !indicator.classList.contains('disabled')) {
            try { recognition.start(); } catch (e) {}
        }
    };

    function handleCommand(command) {
        if (command.includes('дублікат')) {
            const btn = document.getElementById('btn-delete-duplicates');
            if (btn) { 
                console.log('Duplicate removal triggered via voice');
                const feedback = document.getElementById('voice-feedback');
                if (feedback) feedback.innerHTML = "🧹 Видаляю дублікати...";
                btn.form.submit();
                return; 
            }
        }

        const idMatch = command.match(/\d+/);
        const targetId = idMatch ? idMatch[0] : null;

        if (command.includes('видали')) {
            if (targetId) {
                const idBadges = Array.from(document.querySelectorAll('.badge'));
                const targetBadge = idBadges.find(b => b.textContent.trim() == targetId || b.textContent.includes('ID: ' + targetId));
                if (targetBadge) {
                    const row = targetBadge.closest('tr') || targetBadge.closest('.list-group-item');
                    if (row) {
                        const deleteBtn = row.querySelector('a[href*="Delete"], button[type="submit"]');
                        if (deleteBtn) { deleteBtn.click(); return; }
                    }
                }
            }
        }

        if (command.includes('редагувати')) {
            if (targetId) {
                const idBadges = Array.from(document.querySelectorAll('.badge'));
                const targetBadge = idBadges.find(b => b.textContent.trim() == targetId || b.textContent.includes('ID: ' + targetId));
                if (targetBadge) {
                    const row = targetBadge.closest('tr');
                    if (row) {
                        const editBtn = row.querySelector('a[href*="Edit"]');
                        if (editBtn) { editBtn.click(); return; }
                    }
                }
            }
        }

        if (command.includes('зберегти') || command.includes('готово')) {
            const submitBtn = document.querySelector('button[type="submit"]');
            if (submitBtn) { submitBtn.click(); return; }
        }

        if (command.includes('імпорт')) {
            const fileInput = document.querySelector('input[type="file"]');
            if (fileInput) { fileInput.click(); return; }
        }

        if (window.location.href.includes('Edit') || window.location.href.includes('Create')) {
            let field = null;
            let value = "";
            if (command.includes('предмет')) { field = 'Subject'; value = command.replace('предмет', '').trim(); }
            else if (command.includes('час')) { field = 'Time'; value = command.replace('час', '').trim(); }
            else if (command.includes('аудиторія') || command.includes('кабінет')) { field = 'Room'; value = command.replace(/аудиторія|кабінет/, '').trim(); }
            else if (command.includes('дата')) { field = 'Date'; value = command.replace('дата', '').trim(); }
            else if (command.includes('тиждень')) { field = command.includes('2') || command.includes('знаменник') ? '2' : '1'; document.querySelector('[name="Week"]').value = field; return; }
            else if (command.includes('тип')) { 
                const select = document.querySelector('[name="LessonType"]');
                if (command.includes('лекція')) select.value = 'Лекція';
                else if (command.includes('практика')) select.value = 'Практика';
                else if (command.includes('лабораторна')) select.value = 'Лабораторна';
                return;
            }
            else if (command.includes('викладач')) {
                const teacherName = command.replace('викладач', '').trim().toLowerCase();
                const select = document.querySelector('select[name="TeacherId"]');
                const match = Array.from(select.options).find(o => o.textContent.toLowerCase().includes(teacherName));
                if (match) { select.value = match.value; return; }
            }
            else if (command.includes('груп')) {
                const groupName = command.replace(/група|групи|груп/, '').trim().toLowerCase();
                const select = document.querySelector('select[name="selectedGroups"]');
                const match = Array.from(select.options).find(o => o.textContent.toLowerCase().includes(groupName));
                if (match) { match.selected = true; return; }
            }

            if (field && value) {
                const input = document.querySelector(`[name="${field}"]`);
                if (input) {
                    if (input.type === 'date') {
                        let date = new Date();
                        if (value.includes('завтра')) date.setDate(date.getDate() + 1);
                        input.value = date.toISOString().split('T')[0];
                    } else { input.value = value; }
                    return;
                }
            }
        }

        if (command.includes('створити') || (command.includes('додати') && (command.includes('груп') || command.includes('викладач')))) {
            if (command.includes('груп')) {
                let val = command.replace('створити', '').replace('додати', '').replace('групу', '').replace('група', '').trim().toUpperCase();
                const input = document.querySelector('form[action*="AddGroup"] input[name="name"]');
                const form = document.querySelector('form[action*="AddGroup"]');
                if (input && form && val) { input.value = val; form.submit(); return; }
            }
            if (command.includes('викладач')) {
                let val = command.replace('створити', '').replace('додати', '').replace('викладача', '').replace('викладач', '').trim();
                if (val) {
                    val = val.split(' ').map(word => word.charAt(0).toUpperCase() + word.slice(1)).join(' ');
                    const input = document.querySelector('form[action*="AddTeacher"] input[name="fullName"]');
                    const form = document.querySelector('form[action*="AddTeacher"]');
                    if (input && form) { input.value = val; form.submit(); return; }
                }
            }
        }

        if (command.includes('додати заняття') || command.includes('створити заняття')) {
            const btn = document.querySelector('a[href*="Create"]');
            if (btn) btn.click();
            return;
        } 
        
        if (command.includes('словник') || command.includes('викладач') || command.includes('груп')) {
            if (!command.includes('пошук') && !command.includes('створити') && !command.includes('додати') && !command.includes('видалити') && !command.includes('редагувати')) {
                const btn = document.querySelector('a[href*="Dictionaries"]');
                if (btn) btn.click();
                return;
            }
        }

        if (command.includes('експорт')) {
            const btn = document.querySelector('a[href*="Export"]');
            if (btn) btn.click();
            return;
        }

        if (command.includes('очистити')) {
            const links = Array.from(document.querySelectorAll('a'));
            const clearLink = links.find(a => a.textContent.includes('Очистити'));
            if (clearLink) clearLink.click();
            return;
        }

        if (command.includes('пошук') || command.includes('знайти')) {
            const cleanCommand = command.replace('пошук', '').replace('знайти', '').trim();
            const searchForm = document.querySelector('form[method="get"]');
            if (!searchForm) return;

            let fieldName = 'searchSubject';
            let value = cleanCommand;

            if (cleanCommand.includes('дата') || cleanCommand.includes('день') || cleanCommand.includes('сьогодні') || cleanCommand.includes('завтра')) {
                fieldName = 'searchDate';
                if (cleanCommand.includes('завтра')) value = 'завтра';
                else if (cleanCommand.includes('сьогодні')) value = 'сьогодні';
                else value = cleanCommand.replace('дата', '').replace('день', '').trim();
            }
            else if (cleanCommand.includes('викладач')) { fieldName = 'searchTeacher'; value = cleanCommand.replace(/викладач|вчитель/, '').trim(); }
            else if (cleanCommand.includes('груп')) { fieldName = 'searchGroup'; value = cleanCommand.replace(/група|групи|груп/, '').trim(); }
            else if (cleanCommand.includes('час')) { fieldName = 'searchTime'; value = cleanCommand.replace('час', '').trim(); }
            else if (cleanCommand.includes('аудитор') || cleanCommand.includes('кабінет') || cleanCommand.includes('номер')) { fieldName = 'searchRoom'; value = cleanCommand.replace(/аудиторія|аудиторію|кабінет|номер/, '').trim(); }
            else if (cleanCommand.includes('тиждень')) { fieldName = 'searchWeek'; value = cleanCommand.replace('тиждень', '').trim(); }
            else if (cleanCommand.includes('тип')) { 
                fieldName = 'searchLessonType'; 
                value = cleanCommand.replace('тип', '').trim();
                if (value.includes('лекція')) value = 'Лекція';
                else if (value.includes('практика')) value = 'Практика';
                else if (value.includes('лабораторна')) value = 'Лабораторна';
            }

            const input = document.querySelector(`input[name="${fieldName}"], select[name="${fieldName}"]`);
            if (input) { input.value = value; searchForm.submit(); }
            return;
        }

        if (command.includes('назад') || command.includes('головна')) { window.location.href = '/Lesson'; }
    }

    function updateIndicator(active) {
        const indicator = document.getElementById('voice-indicator');
        if (!indicator) return;
        if (active) {
            indicator.innerHTML = '<div class="pulse"></div> 🎤 Голос: Слухаю...';
            indicator.classList.remove('disabled');
        } else {
            indicator.innerHTML = '🎤 Голос: Вимкнено';
            indicator.classList.add('disabled');
        }
    }

    function initVoice() {
        if (document.getElementById('voice-indicator')) return;
        const feedback = document.createElement('div');
        feedback.id = 'voice-feedback';
        feedback.style.cssText = 'position:fixed; bottom:80px; right:30px; background:rgba(0,0,0,0.7); color:white; padding:10px 20px; border-radius:10px; z-index:10001; display:none; font-size:0.9rem;';
        document.body.appendChild(feedback);

        const indicator = document.createElement('div');
        indicator.id = 'voice-indicator';
        indicator.innerHTML = '🎤 Натисніть, щоб увімкнути';
        indicator.classList.add('disabled');
        indicator.onclick = () => {
            if (indicator.classList.contains('disabled')) { try { recognition.start(); } catch (e) {} }
            else { recognition.stop(); updateIndicator(false); }
        };
        document.body.appendChild(indicator);
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => {
        if (!window.location.href.includes('VoiceTest')) initVoice();
    });
    else {
        if (!window.location.href.includes('VoiceTest')) initVoice();
    }
}
