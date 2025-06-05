export interface ICard {    
    id: string;
    listId: string;
    swimlaneId: string;
    boardId: string;
    title: string;
    color: string;
    description: string | null;
    tags: number[];
    assignedUsers: number[];
    dueDate: string | null;
}
