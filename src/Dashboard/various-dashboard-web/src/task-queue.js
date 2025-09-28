class TaskQueue {
    constructor() {
        // 任务队列
        this.queue = [];
        // 当前正在处理的任务
        this.currentTask = null;
        // 事件回调存储
        this.events = {
            // 有任务需要处理时触发
            taskReady: [],
            // 所有任务处理完成时触发
            allCompleted: [],
            // 队列状态变化时触发（添加任务、完成任务等）
            statusChange: []
        };
    }

    /**
     * 绑定事件监听
     * @param {string} eventName 事件名称
     * @param {Function} callback 回调函数
     */
    on(eventName, callback) {
        if (this.events[eventName]) {
            this.events[eventName].push(callback);
        }
    }

    /**
     * 移除事件监听
     * @param {string} eventName 事件名称
     * @param {Function} callback 要移除的回调函数
     */
    off(eventName, callback) {
        if (this.events[eventName]) {
            this.events[eventName] = this.events[eventName].filter(cb => cb !== callback);
        }
    }

    /**
     * 触发事件
     * @param {string} eventName 事件名称
     * @param  {...any} args 传递给回调的参数
     */
    emit(eventName, ...args) {
        if (this.events[eventName]) {
            this.events[eventName].forEach(callback => {
                callback(...args);
            });
        }
    }

    /**
     * 添加任务到队列
     * @param {any} task 任务数据，可以是任意类型
     */
    enqueue(task) {
        this.queue.push(task);
        this.emit('statusChange', {
            action: 'enqueue',
            queueLength: this.queue.length,
            currentTask: this.currentTask
        });

        // 如果当前没有正在处理的任务，立即通知有任务可处理
        if (!this.currentTask && this.queue.length === 1) {
            this.notifyNextTask();
        }
    }

    /**
     * 通知处理下一个任务
     */
    notifyNextTask() {
        if (this.queue.length === 0) {
            this.currentTask = null;
            this.emit('allCompleted');
            return;
        }

        // 获取下一个任务
        this.currentTask = this.queue.shift();

        // 触发任务就绪事件，通知外部处理
        this.emit('taskReady', this.currentTask, (result) => {
            // 这个回调函数供外部调用，以通知任务处理完成
            this.currentTask = null;
            this.emit('statusChange', {
                action: 'taskCompleted',
                queueLength: this.queue.length,
                result: result
            });
            // 处理完当前任务后，通知下一个任务
            this.notifyNextTask();
        });
    }

    /**
     * 获取当前队列长度
     * @returns {number} 队列长度
     */
    getLength() {
        return this.queue.length;
    }

    /**
     * 清空队列
     */
    clear() {
        this.queue = [];
        this.currentTask = null;
        this.emit('statusChange', {
            action: 'clear',
            queueLength: 0
        });
    }
}

// 导出单例实例
export const taskQueue = new TaskQueue();

// 导出类供创建多个实例
export default TaskQueue;
